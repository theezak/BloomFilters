using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    /// <summary>
    /// The strata estimator helps estimate the number of differences between two (sub)sets.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>For higher strata's, MinWise gives a better accurace/size balance.</remarks>
    public class StrataEstimator<T, TId, TCount>
        where TCount : struct
    {
        protected readonly Func<TId, long> _idHash;
        private readonly long _capacity;
        protected readonly InvertibleBloomFilter<T, TId, TCount>[] _strataFilters = 
            new InvertibleBloomFilter<T,TId,TCount>[_maxTrailingZeros];
        protected readonly IBloomFilterConfiguration<T, int, TId, long,TCount> _configuration;
        protected const byte _maxTrailingZeros = sizeof(long)*8;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="idHash"></param>
        public StrataEstimator(long capacity, IBloomFilterConfiguration<T,int,TId,long,TCount> configuration)
        {
            _idHash = id => configuration.IdHashes(id, 1).First();
            _capacity = capacity;
            _configuration = configuration;
        }

        public StrataEstimatorData<TId, TCount> Extract()
        {
            var result = new StrataEstimatorData<TId, TCount> {
                Capacity = _capacity,
                BloomFilters = new InvertibleBloomFilterData<TId, TCount>[_maxTrailingZeros]
            };
            for(int i=0; i < _strataFilters.Length; i++)
            {
                if (_strataFilters[i] == null) continue;
               result.BloomFilters[i] = _strataFilters[i].Extract();
            }
            return result;
        }

        /// <summary>
        /// Add an item to strata filter.
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(T item)
        {
            Add(item, NumTrailingBinaryZeros(_idHash(_configuration.GetId(item))));
        }

        protected void Add(T item, long idx)
        {
            if (_strataFilters[idx] == null)
            {
                _strataFilters[idx] = new InvertibleBloomFilter<T, TId, TCount>(_capacity, _configuration);
            }
            _strataFilters[idx].Add(item);
        }

        public virtual void Remove(T item)
        {
            Remove(item, NumTrailingBinaryZeros(_idHash(_configuration.GetId(item))));
        }

        protected void Remove(T item, long idx)
        {
            _strataFilters[idx]?.Remove(item);
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns></returns>
        public virtual ulong Decode(StrataEstimator<T, TId, TCount> estimator)
        {
            ulong count = 0L;
            if (estimator == null ||
                estimator._capacity != _capacity ||
                estimator._strataFilters.Length != _strataFilters.Length) return count;
            var setA = new HashSet<TId>();       
            for(int i = _strataFilters.Length-1; i >= 0; i--)
            {
                var ibf = _strataFilters[i];
                var estimatorIbf = estimator._strataFilters[i];
                if (ibf == null && estimatorIbf == null) continue;
                if (ibf == null || estimatorIbf == null)
                {
                     return (ulong)(Math.Pow(2, i+1)*DecodeCountFactor*Math.Max(setA.Count, 1));
                }
                ibf.Subtract(estimatorIbf);
                if (!ibf.Decode(setA, setA, setA))
                {
                    return (ulong)(Math.Pow(2, i+1) * DecodeCountFactor * Math.Max(setA.Count, 1));
                }
               
            }
            return (ulong)(DecodeCountFactor * setA.LongCount());
        }
        
       protected virtual double DecodeCountFactor
        {
            get
            {
                return _capacity >= 20? 1.39D : 1.0D;
            }
        }

      protected static int NumTrailingBinaryZeros(long n)
        {
            int mask = 1;
            for (int i = 0; i < _maxTrailingZeros; i++, mask <<= 1)
                if ((n & mask) != 0)
                    return i;

            return _maxTrailingZeros;
        }
    }
}

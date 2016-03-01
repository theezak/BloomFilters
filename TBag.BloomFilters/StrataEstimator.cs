using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    /// <summary>
    /// The strate estimator helps estimate the number of differences between two (sub)sets.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <remarks>For higher strata's, MinWise gives a better accurace/size balance.</remarks>
    public class StrataEstimator<T, TId>
    {
        private readonly Func<TId, int> _idHash;
        private readonly Func<T, TId> _getEntityId;
        private readonly int _capacity;
        private readonly IDictionary<int, InvertibleBloomFilter<T, TId>> _strataFilters = 
            new Dictionary<int, InvertibleBloomFilter<T,TId>>();
        private readonly IBloomFilterConfiguration<T, int, TId, int> _configuration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="idHash"></param>
        public StrataEstimator(int capacity, IBloomFilterConfiguration<T,int,TId,int> configuration)
        {
            _idHash = id => configuration.IdHashes(id, 1).First();
            _getEntityId = configuration.GetId;
            _capacity = capacity;
            _configuration = configuration;
        }

        public void Add(T item)
        {
            var idx = NumTrailingBinaryZeros(_idHash(_getEntityId(item)));
            var ibf = default(InvertibleBloomFilter<T, TId>);
            if (_strataFilters.TryGetValue(idx, out ibf))
            {
                ibf.Add(item);
            }
            else
            {
                _strataFilters[idx] = new InvertibleBloomFilter<T, TId>(_capacity, _configuration);
                _strataFilters[idx].Add(item);
            }
        }

        public int Decode(StrataEstimator<T, TId> estimator)
        {
            var count = 0;
            if (estimator == null ||
                estimator._capacity != _capacity ||
                estimator._strataFilters.Count != _strataFilters.Count) return count;
            var setA = new HashSet<TId>();
        var setB = new HashSet<TId>();
        var setC = new HashSet<TId>();
            for(int i = _strataFilters.Count-1; i >= 0; i--)
            {
                var ibfPair = _strataFilters.ElementAt(i);
                var estimatorIbf = default(InvertibleBloomFilter<T, TId>);
                if (!estimator._strataFilters.TryGetValue(ibfPair.Key, out estimatorIbf))
                {
                    return (int)(Math.Pow(2, i+1)*DecodeCountFactor*(setA.Count + setB.Count + setC.Count));
                }
                ibfPair.Value.Subtract(estimatorIbf);
                if (!ibfPair.Value.Decode(setA, setB, setC))
                {
                    return (int)(Math.Pow(2, i+1) * DecodeCountFactor * (setA.Count + setB.Count + setC.Count));
                }
               
            }
            return (int)(DecodeCountFactor * (setA.Count + setB.Count + setC.Count));
        }
        
        private double DecodeCountFactor
        {
            get
            {
                return _capacity > 80 ? 1.39D : 1.0D;
            }
        }

      private static int NumTrailingBinaryZeros(int n)
        {
            int mask = 1;
            for (int i = 0; i < 32; i++, mask <<= 1)
                if ((n & mask) != 0)
                    return i;

            return 32;
        }
    }
}

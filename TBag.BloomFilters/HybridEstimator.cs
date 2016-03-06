using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TBag.BloomFilters
{
    [DataContract, Serializable]
    public class HybridEstimatorData<TId>
    {
        /// <summary>
        /// The capacity
        /// </summary>
        [DataMember(Order = 1)]
        public ulong Capacity { get; set; }

        [DataMember(Order = 2)]
        public StrataEstimatorData<TId> StrataEstimator { get; set; }

        [DataMember(Order = 3)]
        public int StrataCount { get; set; }

        [DataMember(Order = 4)]
        public BitMinwiseHashEstimatorData BitMinwiseEstimator { get; set; }
    }

    /// <summary>
    /// A hybrid estimator with a limited strata that cuts over to a bit minwise estimator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class HybridEstimator<T, TId> : StrataEstimator<T, TId>
    {
        private readonly BitMinwiseHashEstimator<T, TId> _minwiseEstimator;
        private readonly int _maxStrata;
        private readonly ulong _capacity;
        private readonly ulong _setSize;

          /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity">Capacity for strata estimator (good default is 80)</param>
        /// <param name="bitSize">The bit size for the bit minwise estimator.</param>
        /// <param name="minWiseHashCount">number of hash functions for the bit minwise estimator</param>
        /// <param name="setSize">Estimated maximum set size for the bit minwise estimator (capacity)</param>
        /// <param name="maxStrata">Maximum strate for the strata estimator.</param>
        /// <param name="configuration">The configuration</param>
        /// <remarks>TODO: clean up configuration</remarks>
        public HybridEstimator(
            ulong capacity,
            byte bitSize,
            int minWiseHashCount,
            ulong setSize,
            byte maxStrata,
            IBloomFilterConfiguration<T, int, TId, long> configuration) :
            base(capacity, configuration)
        {
            _capacity = capacity;
            _maxStrata = maxStrata;
            var max = Math.Pow(2, _maxTrailingZeros);
            var inStrata = max - Math.Pow(2, _maxTrailingZeros - maxStrata);
            //TODO: clean up math.
            _setSize = (uint)(setSize * (1-(inStrata / max)));
            _minwiseEstimator = new BitMinwiseHashEstimator<T, TId>(configuration, bitSize, minWiseHashCount, Math.Max(_setSize,1));
        }

        /// <summary>
        /// Add an item to the estimator.
        /// </summary>
        /// <param name="item"></param>
        public override void Add(T item)
        {
            var idx = NumTrailingBinaryZeros(_idHash(_configuration.GetId(item)));
            if (idx < _maxStrata)
            {
                base.Add(item, idx);
            }
            else
            {
                _minwiseEstimator.Add(item);
            }
        }

        /// <summary>
        /// Extract the hybrid estimator in a serializable format.
        /// </summary>
        /// <returns></returns>
        public HybridEstimatorData<TId> ExtractHybrid()
        {
            return new HybridEstimatorData<TId>
            {
                Capacity = _capacity,
                BitMinwiseEstimator = _minwiseEstimator.Extract(),
                StrataEstimator = Extract(),
                StrataCount = _maxStrata
            };
        }

        protected override double DecodeCountFactor => _capacity >= 20 ? 1.45D : 1.0D;

        /// <summary>
        /// Decode the given hybrid estimator.
        /// </summary>
        /// <param name="estimator"></param>
        /// <returns></returns>
        public ulong Decode(HybridEstimator<T, TId> estimator)
        {
            var strataSize = base.Decode(estimator);
            var minWiseSize =  (ulong)(_setSize - (_minwiseEstimator.Similarity(estimator._minwiseEstimator) * _setSize));
            return strataSize + minWiseSize;
        }
    }
}

namespace TBag.BloomFilters.Estimators
{
    /// <summary>
    /// Implementation of <see cref="IBitMinwiseHashEstimatorDataFactory"/>
    /// </summary>
    public class BitMinwiseHashEstimatorDataFactory : IBitMinwiseHashEstimatorDataFactory
    {
        /// <summary>
        /// Create an instance of <see cref="IBitMinwiseHashEstimatorData"/>
        /// </summary>
        /// <param name="bitSize"></param>
        /// <param name="capacity"></param>
        /// <param name="hashCount"></param>
        /// <returns></returns>
        public IBitMinwiseHashEstimatorData Create(
            byte bitSize, 
            long capacity, 
            int hashCount)
        {
            var valuesSize = bitSize*capacity/8;
            if (valuesSize % 8 != 0)
            {
                valuesSize++;
            }
            return new BitMinwiseHashEstimatorData
            {
                BitSize = bitSize,
                Capacity = capacity,
                HashCount = hashCount,
                Values = new byte[valuesSize]
            };
        }
    }
}

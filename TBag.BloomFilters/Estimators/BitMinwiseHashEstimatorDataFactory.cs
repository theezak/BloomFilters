

namespace TBag.BloomFilters.Estimators
{
    public class BitMinwiseHashEstimatorDataFactory : IBitMinwiseHashEstimatorDataFactory
    {
        public IBitMinwiseHashEstimatorData Create(byte bitSize, long capacity, int hashCount)
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

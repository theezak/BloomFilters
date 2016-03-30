namespace TBag.BloomFilters.Estimators
{
    public interface IBitMinwiseHashEstimatorDataFactory
    {
        IBitMinwiseHashEstimatorData Create(byte bitSize, long capacity, int hashCount);
    }
}
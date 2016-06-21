using System;
using System.Runtime.Serialization;

namespace TBag.BloomFilters.Countable
{
    [DataContract, Serializable]
    class CountingBloomFilterData<TId, TCount> : ICountingBloomFilterData<TId, TCount>
        where TId : struct
        where TCount : struct
    {
        public byte[] Bits
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public long BlockSize
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public long Capacity
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ICompressedArray<TCount> CountProvider
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TCount[] Counts
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public uint HashFunctionCount
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public long ItemCount
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void SyncCompressionProviders<THash>(Configurations.ICountingBloomFilterConfiguration<TId, THash, TCount> configuration) where THash : struct
        {
            throw new NotImplementedException();
        }
    }
}

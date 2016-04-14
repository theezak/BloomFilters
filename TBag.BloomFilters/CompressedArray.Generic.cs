namespace TBag.BloomFilters
{
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Compressed representation of counters.
    /// </summary>
    /// <typeparam name="TCount"></typeparam>
    /// <remarks>Based upon a membership test, reduce the number of value stored. We don't actually reduce memory usage (utilizing a dictionary impacts performance and payback is low, since IBFs get utilized beyond their regular capacity for set differences), but we do reduce the serialized size.</remarks>
    internal class CompressedArray<TCount> : ICompressedArray<TCount> where TCount : struct
    {
        private static readonly TCount[] Empty = new TCount[0];
         private Func<long, bool> _membershipTest;
        private TCount[] _values;
      
        /// <summary>
        /// Indexer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TCount this[long index] 
        {
            get { return _values[index]; }

            set { _values[index] = value; }
        }        

        /// <summary>
        /// Load the counters.
        /// </summary>
        /// <param name="values">The counters</param>
        /// <param name="blockSize">Block size</param>
         /// <param name="membershipTest">Optional member ship test</param>
        /// <remarks>When <see cref="membershipTest"/> is null and the counters are the same length as the block size, an array will be used. Otherwise, a dictionary will be used with a sparse array based upon membership (removing 0 count). The membership could be tested against for example the idSum or hashSum (when not 0, a count should exist)</remarks>
        public void Load(
            TCount[] values, 
            long blockSize,
            Func<long, bool> membershipTest = null
            )
        {
            _membershipTest = membershipTest;
            if (values == null)
            {
                _values = new TCount[blockSize];
                return;
            }
            if (values.LongLength == blockSize)
            {
                _values = values;
                return;
            }
            _values = new TCount[blockSize];
            //very basic deflate.
            membershipTest = membershipTest ?? (position => true);
            var counterIdx = 0L;
            for (var i = 0L; i < blockSize && counterIdx < values.Length; i++)
            {
                if (!membershipTest(i)) continue;
                _values[i] = values[counterIdx];
                counterIdx++;
            }
        }

        public IEnumerator<TCount> GetEnumerator()
        {
            if (_values == null) return Empty.AsEnumerable().GetEnumerator();
            return _membershipTest == null ? 
                _values.AsEnumerable().GetEnumerator() : 
                _values.Where((v, i) => _membershipTest(i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_values == null) return Empty.GetEnumerator();
            return _membershipTest == null ?
                _values.GetEnumerator() :
                _values.Where((v, i) => _membershipTest(i)).GetEnumerator();
        }
    }
}

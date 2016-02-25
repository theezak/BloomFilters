using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TBag.HashAlgorithms
{
    /// <summary>
    /// MersenneTwister
    /// </summary>
    public class MersenneTwister : RandomBase
    {

        #region Field

        protected const int N = 624;
         protected const int M = 397;
        protected const uint MATRIX_A = 0x9908b0dfU;
        protected const uint UPPER_MASK = 0x80000000U;
        protected const uint LOWER_MASK = 0x7fffffffU;
        protected const uint TEMPER1 = 0x9d2c5680U;
         protected const uint TEMPER2 = 0xefc60000U;
         protected const int TEMPER3 = 11;
         protected const int TEMPER4 = 7;
         protected const int TEMPER5 = 15;
        protected const int TEMPER6 = 18;

        protected UInt32[] mt;
         protected int mti;
        private UInt32[] mag01;

        #endregion

         public MersenneTwister() : this(Environment.TickCount) { }

        public MersenneTwister(int seed)
        {
            mt = new UInt32[N];
            mti = N + 1;
            mag01 = new UInt32[] { 0x0U, MATRIX_A };
            mt[0] = (UInt32)seed;
            for (int i = 1; i < N; i++)
                mt[i] = (UInt32)(1812433253 * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i);
        }

         public override uint NextUInt32()
        {
            UInt32 y;
            if (mti >= N) { gen_rand_all(); mti = 0; }
            y = mt[mti++];
            y ^= (y >> TEMPER3);
            y ^= (y << TEMPER4) & TEMPER1;
            y ^= (y << TEMPER5) & TEMPER2;
            y ^= (y >> TEMPER6);
            return y;
        }

        protected void gen_rand_all()
        {
            int kk = 1;
            UInt32 y;
            UInt32 p;
            y = mt[0] & UPPER_MASK;
            do
            {
                p = mt[kk];
                mt[kk - 1] = mt[kk + (M - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
                y = p & UPPER_MASK;
            } while (++kk < N - M + 1);
            do
            {
                p = mt[kk];
                mt[kk - 1] = mt[kk + (M - N - 1)] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
                y = p & UPPER_MASK;
            } while (++kk < N);
            p = mt[0];
            mt[N - 1] = mt[M - 1] ^ ((y | (p & LOWER_MASK)) >> 1) ^ mag01[p & 1];
        }

    }
}

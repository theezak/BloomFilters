namespace TBag.HashAlgorithms
{
    using System;

    /// <summary>
    /// MersenneTwister
    /// </summary>
    public class MersenneTwister : RandomBase
    {

        #region Field

        protected const int N = 624;
         protected const int M = 397;
        protected const uint MatrixA = 0x9908b0dfU;
        protected const uint UpperMask = 0x80000000U;
        protected const uint LowerMask = 0x7fffffffU;
        protected const uint Temper1 = 0x9d2c5680U;
         protected const uint Temper2 = 0xefc60000U;
         protected const int Temper3 = 11;
         protected const int Temper4 = 7;
         protected const int Temper5 = 15;
        protected const int Temper6 = 18;

        protected uint[] Mt;
         protected int Mti;
        private readonly uint[] _mag01;

        #endregion

         public MersenneTwister() : this(Environment.TickCount) { }

        public MersenneTwister(int seed)
        {
            Mt = new uint[N];
            Mti = N + 1;
            _mag01 = new[] { 0x0U, MatrixA };
            Mt[0] = (uint)seed;
            for (var i = 1; i < N; i++)
                Mt[i] = (uint)(1812433253 * (Mt[i - 1] ^ (Mt[i - 1] >> 30)) + i);
        }

         public override uint NextUInt32()
        {
            if (Mti >= N) { gen_rand_all(); Mti = 0; }
            var y = Mt[Mti++];
            y ^= y >> Temper3;
            y ^= (y << Temper4) & Temper1;
            y ^= (y << Temper5) & Temper2;
            y ^= y >> Temper6;
            return y;
        }

        protected void gen_rand_all()
        {
            int kk = 1;
             uint p;
            var y = Mt[0] & UpperMask;
            do
            {
                p = Mt[kk];
                Mt[kk - 1] = Mt[kk + (M - 1)] ^ ((y | (p & LowerMask)) >> 1) ^ _mag01[p & 1];
                y = p & UpperMask;
            } while (++kk < N - M + 1);
            do
            {
                p = Mt[kk];
                Mt[kk - 1] = Mt[kk + (M - N - 1)] ^ ((y | (p & LowerMask)) >> 1) ^ _mag01[p & 1];
                y = p & UpperMask;
            } while (++kk < N);
            p = Mt[0];
            Mt[N - 1] = Mt[M - 1] ^ ((y | (p & LowerMask)) >> 1) ^ _mag01[p & 1];
        }

    }
}

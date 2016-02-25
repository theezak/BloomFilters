using System;

namespace TBag.HashAlgorithms
{
    public class Murmur3 : IMurmurHash
    {
        // 128 bit output, 64 bit platform version       
        private const ulong ReadSize = 16;
        private const ulong C1 = 0x87c37b91114253d5L;
        private const ulong C2 = 0x4cf5ad432745937fL;

        byte[] IHashAlgorithm.Hash(byte[] array, uint seed)
        {
            return ComputeMurmurHash(array, seed);
        }

        private static void MixBody(ulong k1, ulong k2, State state)
        {
            state.H1 ^= MixKey1(k1);
            state.H1 = state.H1.RotateLeft(27);
            state.H1 += state.H2;
            state.H1 = state.H1*5 + 0x52dce729;
            state.H2 ^= MixKey2(k2);
            state.H2 = state.H2.RotateLeft(31);
            state.H2 += state.H1;
            state.H2 = state.H2*5 + 0x38495ab5;
        }

        private static ulong MixKey1(ulong k1)
        {
            k1 *= C1;
            k1 = k1.RotateLeft(31);
            k1 *= C2;
            return k1;
        }

        private static ulong MixKey2(ulong k2)
        {
            k2 *= C2;
            k2 = k2.RotateLeft(33);
            k2 *= C1;
            return k2;
        }

        private static ulong MixFinal(ulong k)
        {
            // avalanche bits           
            k ^= k >> 33;
            k *= 0xff51afd7ed558ccdL;
            k ^= k >> 33;
            k *= 0xc4ceb9fe1a85ec53L;
            k ^= k >> 33;
            return k;
        }

        /// <summary>
        ///     Compute the Hash.
        /// </summary>
        /// <param name="bb"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private static byte[] ComputeMurmurHash(byte[] bb, uint seed = 0)
        {
            var state = new State {H1 = seed, Length = 0L};
            ProcessBytes(bb, state);
            return HashValue(state);
        }

        private static void ProcessBytes(byte[] bb, State state)
        {
            int pos = 0;
            var remaining = (ulong) bb.Length;
            // read 128 bits, 16 bytes, 2 longs in eacy cycle         
            while (remaining >= ReadSize)
            {
                ulong k1 = bb.GetUInt64(pos);
                pos += 8;
                ulong k2 = bb.GetUInt64(pos);
                pos += 8;
                state.Length += ReadSize;
                remaining -= ReadSize;
                MixBody(k1, k2, state);
            }
            // if the input MOD 16 != 0       
            if (remaining > 0) ProcessBytesRemaining(bb, remaining, pos, state);
        }

        private static void ProcessBytesRemaining(byte[] bb, ulong remaining, int pos, State state)
        {
            ulong k1 = 0;
            ulong k2 = 0;
            state.Length += remaining;
            // little endian (x86) processing        
            switch (remaining)
            {
                case 15:
                    k2 ^= (ulong) bb[pos + 14] << 48;
                    // fall through                 
                    goto case 14;
                case 14:
                    k2 ^= (ulong) bb[pos + 13] << 40;
                    // fall through               
                    goto case 13;
                case 13:
                    k2 ^= (ulong) bb[pos + 12] << 32;
                    // fall through              
                    goto case 12;
                case 12:
                    k2 ^= (ulong) bb[pos + 11] << 24;
                    // fall through                
                    goto case 11;
                case 11:
                    k2 ^= (ulong) bb[pos + 10] << 16; // fall through                
                    goto case 10;
                case 10:
                    k2 ^= (ulong) bb[pos + 9] << 8; // fall through             
                    goto case 9;
                case 9:
                    k2 ^= bb[pos + 8]; // fall through             
                    goto case 8;
                case 8:
                    k1 ^= bb.GetUInt64(pos);
                    break;
                case 7:
                    k1 ^= (ulong) bb[pos + 6] << 48;
                    // fall through            
                    goto case 6;
                case 6:
                    k1 ^= (ulong) bb[pos + 5] << 40; // fall through            
                    goto case 5;
                case 5:
                    k1 ^= (ulong) bb[pos + 4] << 32; // fall through        
                    goto case 4;
                case 4:
                    k1 ^= (ulong) bb[pos + 3] << 24; // fall through           
                    goto case 3;
                case 3:
                    k1 ^= (ulong) bb[pos + 2] << 16; // fall through          
                    goto case 2;
                case 2:
                    k1 ^= (ulong) bb[pos + 1] << 8; // fall through           
                    goto case 1;
                case 1:
                    k1 ^= bb[pos]; // fall through       
                    break;
                default:
                    throw new InvalidOperationException("Something went wrong with remaining bytes calculation.");
            }
            state.H1 ^= MixKey1(k1);
            state.H2 ^= MixKey2(k2);
        }

        private static byte[] HashValue(State state)
        {
            state.H1 ^= state.Length;
            state.H2 ^= state.Length;
            state.H1 += state.H2;
            state.H2 += state.H1;
            state.H1 = MixFinal(state.H1);
            state.H2 = MixFinal(state.H2);
            state.H1 += state.H2;
            state.H2 += state.H1;
            var hash = new byte[ReadSize];
            Array.Copy(BitConverter.GetBytes(state.H1), 0, hash, 0, 8);
            Array.Copy(BitConverter.GetBytes(state.H2), 0, hash, 8, 8);
            return hash;
        }

        private class State
        {
            public ulong Length;
            // if want to start with a seed, create a constructor     
            public ulong H1;
            public ulong H2;
        }
    }
}
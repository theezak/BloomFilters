using System;
using System.Globalization;
using System.Text;

namespace TBag.HashAlgorithms
{
    /// <summary>
    ///     xxHash algorithm.
    /// </summary>
    public class XxHash : IXxHash
    {
        private const uint Prime321 = 2654435761U;
        private const uint Prime322 = 2246822519U;
        private const uint Prime323 = 3266489917U;
        private const uint Prime324 = 668265263U;
        private const uint Prime325 = 374761393U;

        byte[] IHashAlgorithm.Hash(byte[] array, uint seed)
        {
            return BitConverter.GetBytes(Digest(Update(
                array,
                array.Length,
                Init(new XXH_State(), seed))));
        }

        //private static uint CalculateHash(byte[] buf, int len = -1, uint seed = 0)
        //{
        //    uint h32;
        //    int index = 0;
        //    if (len == -1)
        //    {
        //        len = buf.Length;
        //    }


        //    if (len >= 16)
        //    {
        //        int limit = len - 16;
        //        uint v1 = seed + PRIME32_1 + PRIME32_2;
        //        uint v2 = seed + PRIME32_2;
        //        uint v3 = seed + 0;
        //        uint v4 = seed - PRIME32_1;

        //        do
        //        {
        //            v1 = CalcSubHash(v1, buf, index);
        //            index += 4;
        //            v2 = CalcSubHash(v2, buf, index);
        //            index += 4;
        //            v3 = CalcSubHash(v3, buf, index);
        //            index += 4;
        //            v4 = CalcSubHash(v4, buf, index);
        //            index += 4;
        //        } while (index <= limit);

        //        h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
        //    }
        //    else
        //    {
        //        h32 = seed + PRIME32_5;
        //    }

        //    h32 += (uint)len;

        //    while (index <= len - 4)
        //    {
        //        h32 += BitConverter.ToUInt32(buf, index) * PRIME32_3;
        //        h32 = RotateLeft(h32, 17) * PRIME32_4;
        //        index += 4;
        //    }

        //    while (index < len)
        //    {
        //        h32 += buf[index] * PRIME32_5;
        //        h32 = RotateLeft(h32, 11) * PRIME32_1;
        //        index++;
        //    }

        //    h32 ^= h32 >> 15;
        //    h32 *= PRIME32_2;
        //    h32 ^= h32 >> 13;
        //    h32 *= PRIME32_3;
        //    h32 ^= h32 >> 16;

        //    return h32;
        //}

        private static XXH_State Init(XXH_State state, uint seed = 0)
        {
            if (state.Memory != null) return state;
            state.Seed = seed;
            state.V1 = seed + Prime321 + Prime322;
            state.V2 = seed + Prime322;
            state.V3 = seed + 0;
            state.V4 = seed - Prime321;
            state.TotalLen = 0;
            state.Memsize = 0;
            state.Memory = new byte[16];
            return state;
        }

        private static XXH_State Update(byte[] input, int len, XXH_State state)
        {
            int index = 0;
            state.TotalLen += (uint) len;

            if (state.Memsize + len < 16)
            {
                Array.Copy(input, 0, state.Memory, state.Memsize, len);
                state.Memsize += len;
                return state;
            }

            if (state.Memsize > 0)
            {
                Array.Copy(input, 0, state.Memory, state.Memsize, 16 - state.Memsize);

                state.V1 = CalcSubHash(state.V1, state.Memory, index);
                index += 4;
                state.V2 = CalcSubHash(state.V2, state.Memory, index);
                index += 4;
                state.V3 = CalcSubHash(state.V3, state.Memory, index);
                index += 4;
                state.V4 = CalcSubHash(state.V4, state.Memory, index);
                index += 4;
                index = 0;
                state.Memsize = 0;
            }

            if (index <= len - 16)
            {
                int limit = len - 16;
                uint v1 = state.V1;
                uint v2 = state.V2;
                uint v3 = state.V3;
                uint v4 = state.V4;

                do
                {
                    v1 = CalcSubHash(v1, input, index);
                    index += 4;
                    v2 = CalcSubHash(v2, input, index);
                    index += 4;
                    v3 = CalcSubHash(v3, input, index);
                    index += 4;
                    v4 = CalcSubHash(v4, input, index);
                    index += 4;
                } while (index <= limit);

                state.V1 = v1;
                state.V2 = v2;
                state.V3 = v3;
                state.V4 = v4;
            }

            if (index < len)
            {
                Array.Copy(input, index, state.Memory, 0, len - index);
                state.Memsize = len - index;
            }
            return state;
        }

        private static uint Peek4(byte[] buffer, int offset)
        {
            // NOTE: It's faster than BitConverter.ToUInt32 (suprised? me too)
            return
                ((uint)buffer[offset]) |
                ((uint)buffer[offset + 1] << 8) |
                ((uint)buffer[offset + 2] << 16) |
                ((uint)buffer[offset + 3] << 24);
        }

        private static uint Digest(XXH_State state)
        {
            uint h32;
            int index = 0;
            if (state.TotalLen >= 16)
            {
                h32 = RotateLeft(state.V1, 1) +
                      RotateLeft(state.V2, 7) +
                      RotateLeft(state.V3, 12) +
                      RotateLeft(state.V4, 18);
            }
            else
            {
                h32 = state.Seed + Prime325;
            }

            h32 += (UInt32) state.TotalLen;

            while (index <= state.Memsize - 4)
            {
                h32 +=  Peek4(state.Memory, index)*Prime323;
                h32 = RotateLeft(h32, 17)*Prime324;
                index += 4;
            }

            while (index < state.Memsize)
            {
                h32 += state.Memory[index]*Prime325;
                h32 = RotateLeft(h32, 11)*Prime321;
                index++;
            }

            h32 ^= h32 >> 15;
            h32 *= Prime322;
            h32 ^= h32 >> 13;
            h32 *= Prime323;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint CalcSubHash(uint value, byte[] buf, int index)
        {
            var readValue = Peek4(buf, index);
            value += readValue*Prime322;
            value = RotateLeft(value, 13);
            value *= Prime321;
            return value;
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }

        private class XXH_State
        {
            public byte[] Memory;
            public int Memsize;
            public uint Seed;
            public ulong TotalLen;
            public uint V1;
            public uint V2;
            public uint V3;
            public uint V4;
        };
    }
}
// Copyright (c) 2005, David Beaumont
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice,
//   this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
// * Neither the name of the copyright holder nor the names of the contributors
//   may be used to endorse or promote products derived from this software
//   without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

namespace AionLightning.Commons.Utils
{
    public class MTRandom
    {
        private const int UpperMask = -2147483648;
        private const int LowerMask = 2147483647;
        private const int N = 624;
        private const int M = 397;
        private static readonly int[] Magic = new int[2]
        {
      0,
      -1727483681
        };
        private const int MagicFactor1 = 1812433253;
        private const int MagicFactor2 = 1664525;
        private const int MagicFactor3 = 1566083941;
        private const int MagicMask1 = -1658038656;
        private const int MagicMask2 = -272236544;
        private const int MagicSeed = 19650218;
        private const long DefaultSeed = 5489;
        private int[] _mt = new int[624];
        private int _mti;
        private bool _compat;
        private int[]? _ibuf;

        public MTRandom()
          : this(false)
        {
        }

        public MTRandom(bool compatible)
        {
            _compat = compatible;
            SetSeed(_compat ? 5489L : DateTime.Now.Ticks);
        }

        public MTRandom(long seed) => SetSeed(seed);

        public MTRandom(byte[] buf) => SetSeed(buf);

        public MTRandom(int[] buf) => SetSeed(buf);

        private void SetSeed(int seed)
        {
            if (_mt == null)
                _mt = new int[624];
            _mt[0] = seed;
            for (_mti = 1; _mti < 624; ++_mti)
                _mt[_mti] = 1812433253 * (_mt[_mti - 1] ^ _mt[_mti - 1] >> 30) + _mti;
        }

        public void SetSeed(long seed)
        {
            if (_compat)
            {
                SetSeed((int)seed);
            }
            else
            {
                if (_ibuf == null)
                    _ibuf = new int[2];
                _ibuf[0] = (int)seed;
                _ibuf[1] = (int)(seed >> 32);
                SetSeed(_ibuf);
            }
        }

        public void SetSeed(byte[] buf) => SetSeed(Pack(buf));

        public void SetSeed(int[] buf)
        {
            int length = buf.Length;
            if (length == 0)
                throw new ArgumentException("Seed buffer may not be empty");
            int i = 1;
            int j = 0;
            int num = 624 > length ? 624 : length;
            SetSeed(19650218);
            for (; num > 0; --num)
            {
                _mt[i] = (_mt[i] ^ (_mt[i - 1] ^ _mt[i - 1] >> 30) * 1664525) + buf[j] + j;
                ++i;
                ++j;
                if (i >= 624)
                {
                    _mt[0] = _mt[623];
                    i = 1;
                }
                if (j >= length)
                    j = 0;
            }
            for (int index = 623; index > 0; --index)
            {
                _mt[i] = (_mt[i] ^ (_mt[i - 1] ^ _mt[i - 1] >> 30) * 1566083941) - i;
                ++i;
                if (i >= 624)
                {
                    _mt[0] = _mt[623];
                    i = 1;
                }
            }
            _mt[0] = -2147483648;
        }

        protected int NextBits(int bits)
        {
            int y;

            if (_mti >= N)
            {
                int kk;

                for (kk = 0; kk < N - M; kk++)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + M] ^ (y >> 1) ^ Magic[y & 0x1];
                }
                for (; kk < N - 1; kk++)
                {
                    y = (_mt[kk] & UpperMask) | (_mt[kk + 1] & LowerMask);
                    _mt[kk] = _mt[kk + (M - N)] ^ (y >> 1) ^ Magic[y & 0x1];
                }
                y = (_mt[N - 1] & UpperMask) | (_mt[0] & LowerMask);
                _mt[N - 1] = _mt[M - 1] ^ (y >> 1) ^ Magic[y & 0x1];

                _mti = 0;
            }

            y = _mt[_mti++];

            // Tempering
            y ^= (y >> 11);
            y ^= (y << 7) & MagicMask1;
            y ^= (y << 15) & MagicMask2;
            y ^= (y >> 18);

            return (int)((uint)y >> (32 - bits));
        }

        public static int[] Pack(byte[] buf)
        {
            int k, blen = buf.Length, ilen = ((buf.Length + 3) / 4);
            int[] ibuf = new int[ilen];
            for (int n = 0; n < ilen; n++)
            {
                int m = (n + 1) * 4;
                if (m > blen)
                    m = blen;
                for (k = buf[--m] & 0xff; (m & 0x3) != 0; k = (k << 8) | (buf[--m] & 0xff)) ;
                ibuf[n] = k;
            }
            return ibuf;
        }

        public virtual int Next() => NextBits(32);

        public virtual int Next(int maxValue)
        {
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be positive");
            return (int)(NextDouble() * maxValue);
        }

        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue cannot be greater than maxValue");
            long range = (long)maxValue - minValue + 1;
            return (int)(range * NextDouble()) + minValue;
        }

        public virtual float NextFloat() => (float)NextDouble();

        public virtual double NextDouble()
        {
            ulong a = (ulong)NextBits(26) << 27;
            ulong b = (ulong)NextBits(27);
            return (a + b) / 9007199254740992.0;
        }
    }
}

/*
* Derived from https://github.com/google/google-authenticator-android/blob/master/AuthenticatorApp/src/main/java/com/google/android/apps/authenticator/Base32String.java
*
* Copyright (C) 2016 BravoTango86
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CardGameDef.Decks
{
    public static class Base32
    {
        private static readonly char[] Digits;
        private static readonly int Mask;
        private static readonly int Shift;
        private static readonly Dictionary<char, int> CharMap = new Dictionary<char, int>();
        private const string Separator = "-";

        static Base32()
        {
            Digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
            Mask = Digits.Length - 1;
            Shift = NumberOfTrailingZeros(Digits.Length);
            for (var i = 0; i < Digits.Length; i++) CharMap[Digits[i]] = i;
        }

        private static int NumberOfTrailingZeros(int i)
        {
            // HD, Figure 5-14
            if (i == 0) return 32;
            int n = 31;
            int y = i << 16;
            if (y != 0)
            {
                n = n - 16;
                i = y;
            }

            y = i << 8;
            if (y != 0)
            {
                n = n - 8;
                i = y;
            }

            y = i << 4;
            if (y != 0)
            {
                n = n - 4;
                i = y;
            }

            y = i << 2;
            if (y != 0)
            {
                n = n - 2;
                i = y;
            }

            return n - (int) ((uint) (i << 1) >> 31);
        }

        public static byte[] Decode(string encoded)
        {
            // Remove whitespace and separators
            encoded = encoded.Trim().Replace(Separator, "");

            // Remove padding. Note: the padding is used as hint to determine how many
            // bits to decode from the last incomplete chunk (which is commented out
            // below, so this may have been wrong to start with).
            encoded = Regex.Replace(encoded, "[=]*$", "");

            // Canonicalize to all upper case
            encoded = encoded.ToUpper();
            if (encoded.Length == 0)
            {
                return new byte[0];
            }

            int encodedLength = encoded.Length;
            int outLength = encodedLength * Shift / 8;
            byte[] result = new byte[outLength];
            int buffer = 0;
            int next = 0;
            int bitsLeft = 0;
            foreach (char c in encoded)
            {
                if (!CharMap.ContainsKey(c))
                {
                    throw new DecodingException("Illegal character: " + c);
                }

                buffer <<= Shift;
                buffer |= CharMap[c] & Mask;
                bitsLeft += Shift;
                if (bitsLeft >= 8)
                {
                    result[next++] = (byte) (buffer >> (bitsLeft - 8));
                    bitsLeft -= 8;
                }
            }

            // We'll ignore leftover bits for now.
            //
            // *Commented out code removed*
            return result;
        }


        public static string Encode(byte[] data, bool padOutput = false)
        {
            if (data.Length == 0)
            {
                return "";
            }

            // SHIFT is the number of bits per output character, so the length of the
            // output is the length of the input multiplied by 8/SHIFT, rounded up.
            if (data.Length >= (1 << 28))
            {
                // The computation below will fail, so don't do it.
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            int outputLength = (data.Length * 8 + Shift - 1) / Shift;
            StringBuilder result = new StringBuilder(outputLength);

            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < Shift)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= (data[next++] & 0xff);
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = Shift - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = Mask & (buffer >> (bitsLeft - Shift));
                bitsLeft -= Shift;
                result.Append(Digits[index]);
            }

            if (padOutput)
            {
                int padding = 8 - (result.Length % 8);
                if (padding > 0) result.Append(new string('=', padding == 8 ? 0 : padding));
            }

            return result.ToString();
        }

        public class DecodingException : Exception
        {
            public DecodingException(string message) : base(message)
            {
            }
        }
    }
}

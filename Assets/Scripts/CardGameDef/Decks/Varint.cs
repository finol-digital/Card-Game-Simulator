//THIS CODE ADAPTED FROM
/*
VarintBitConverter: https://github.com/topas/VarintBitConverter
Copyright (c) 2011 Tomas Pastorek, Ixone.cz. All rights reserved.
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:
 1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above
    copyright notice, this list of conditions and the following
    disclaimer in the documentation and/or other materials provided
    with the distribution.
THIS SOFTWARE IS PROVIDED BY TOMAS PASTOREK AND CONTRIBUTORS ``AS IS''
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL TOMAS PASTOREK OR CONTRIBUTORS
BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CardGameDef.Decks
{
    public static class Varint
    {
        private const byte AllButMsb = 0x7f;
        private const byte JustMsb = 0x80;

        public static int PopVarint(List<byte> bytes)
        {
            ulong result = 0;
            var currentShift = 0;
            var bytesPopped = 0;

            for (var i = 0; i < bytes.Count; i++)
            {
                bytesPopped++;
                ulong current = (ulong) bytes[i] & AllButMsb;
                result |= current << currentShift;

                if ((bytes[i] & JustMsb) != JustMsb)
                {
                    bytes.RemoveRange(0, bytesPopped);
                    return (int) result;
                }

                currentShift += 7;
            }

            throw new ArgumentException("Byte array did not contain valid var ints.");
        }

        public static byte[] GetVarint(ulong value)
        {
            byte[] buff = new byte[10];
            var currentIndex = 0;

            if (value == 0)
                return new byte[] {0};

            while (value != 0)
            {
                ulong byteVal = value & AllButMsb;
                value >>= 7;

                if (value != 0)
                    byteVal |= 0x80;

                buff[currentIndex++] = (byte) byteVal;
            }

            byte[] result = new byte[currentIndex];
            Buffer.BlockCopy(buff, 0, result, 0, currentIndex);

            return result;
        }

        public static void Write(MemoryStream memoryStream, int value)
        {
            if (value == 0)
                memoryStream.WriteByte(0);
            else
            {
                byte[] bytes = GetBytes((ulong) value);
                memoryStream.Write(bytes, 0, bytes.Length);
            }
        }

        public static ulong Read(byte[] bytes, ref ulong offset, out int length)
        {
            if (offset > (ulong) bytes.Length)
                throw new ArgumentException("Input is not a valid deck string.");

            ulong value = ReadNext(bytes.Skip((byte) offset).ToArray(), out length);
            offset += (ulong) length;
            return value;
        }

        private static byte[] GetBytes(ulong value)
        {
            using (var memoryStream = new MemoryStream())
            {
                while (value != 0)
                {
                    ulong b = value & 0x7f;
                    value >>= 7;
                    if (value != 0)
                        b |= 0x80;
                    memoryStream.WriteByte((byte) b);
                }

                return memoryStream.ToArray();
            }
        }

        private static ulong ReadNext(byte[] bytes, out int length)
        {
            length = 0;
            ulong result = 0;
            foreach (byte b in bytes)
            {
                ulong value = (ulong) b & 0x7f;
                result |= value << length * 7;
                if ((b & 0x80) != 0x80)
                    break;
                length++;
            }

            length++;
            return result;
        }
    }
}

/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Linq;

namespace CardGameDef.Decks
{
    public static class VarInt
    {
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

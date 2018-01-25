using System.IO;
using System;
using System.Linq;

public static class VarInt
{
    public static byte[] GetBytes(ulong value)
    {
        using (MemoryStream ms = new MemoryStream()) {
            while (value != 0) {
                ulong b = value & 0x7f;
                value >>= 7;
                if (value != 0)
                    b |= 0x80;
                ms.WriteByte((byte)b);
            }
            return ms.ToArray();
        }
    }

    public static ulong ReadNext(byte[] bytes, out int length)
    {
        length = 0;
        ulong result = 0;
        foreach (byte b in bytes) {
            ulong value = (ulong)b & 0x7f;
            result |= value << length * 7;
            if ((b & 0x80) != 0x80)
                break;
            length++;
        }
        length++;
        return result;
    }

    public static void Write(MemoryStream ms, int value)
    {
        if (value == 0)
            ms.WriteByte(0);
        else {
            byte[] bytes = GetBytes((ulong)value);
            ms.Write(bytes, 0, bytes.Length);
        }
    }

    public static ulong Read(byte[] bytes, ref ulong offset, out int length)
    {
        if (offset > (ulong)bytes.Length)
            throw new ArgumentException("Input is not a valid deck string.");

        ulong value = ReadNext(bytes.Skip((byte)offset).ToArray(), out length);
        offset += (ulong)length;
        return value;
    }
}

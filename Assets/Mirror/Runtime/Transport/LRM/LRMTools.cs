using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace LightReflectiveMirror
{
    public static class LRMTools
    {
        public static void WriteByte(this byte[] data, ref int position, byte value)
        {
            data[position] = value;
            position += 1;
        }

        public static byte ReadByte(this byte[] data, ref int position)
        {
            byte value = data[position];
            position += 1;
            return value;
        }

        public static void WriteBool(this byte[] data, ref int position, bool value)
        {
            unsafe
            {
                fixed (byte* dataPtr = &data[position])
                {
                    bool* valuePtr = (bool*)dataPtr;
                    *valuePtr = value;
                    position += 1;
                }
            }
        }

        public static bool ReadBool(this byte[] data, ref int position)
        {
            bool value = BitConverter.ToBoolean(data, position);
            position += 1;
            return value;
        }

        public static void WriteString(this byte[] data, ref int position, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                data.WriteInt(ref position, 0);
            }
            else
            {
                data.WriteInt(ref position, value.Length);
                for (int i = 0; i < value.Length; i++)
                    data.WriteChar(ref position, value[i]);
            }
        }

        public static string ReadString(this byte[] data, ref int position)
        {
            string value = default;

            int stringSize = data.ReadInt(ref position);

            for (int i = 0; i < stringSize; i++)
                value += data.ReadChar(ref position);

            return value;
        }

        public static void WriteBytes(this byte[] data, ref int position, byte[] value)
        {
            data.WriteInt(ref position, value.Length);
            for (int i = 0; i < value.Length; i++)
                data.WriteByte(ref position, value[i]);
        }

        public static byte[] ReadBytes(this byte[] data, ref int position)
        {
            int byteSize = data.ReadInt(ref position);

            byte[] value = new byte[byteSize];

            for (int i = 0; i < byteSize; i++)
                value[i] = data.ReadByte(ref position);

            return value;
        }

        public static void WriteChar(this byte[] data, ref int position, char value)
        {
            unsafe
            {
                fixed (byte* dataPtr = &data[position])
                {
                    char* valuePtr = (char*)dataPtr;
                    *valuePtr = value;
                    position += 2;
                }
            }
        }

        public static char ReadChar(this byte[] data, ref int position)
        {
            char value = BitConverter.ToChar(data, position);
            position += 2;
            return value;
        }

        public static void WriteInt(this byte[] data, ref int position, int value)
        {
            unsafe
            {
                fixed (byte* dataPtr = &data[position])
                {
                    int* valuePtr = (int*)dataPtr;
                    *valuePtr = value;
                    position += 4;
                }
            }
        }

        public static int ReadInt(this byte[] data, ref int position)
        {
            int value = BitConverter.ToInt32(data, position);
            position += 4;
            return value;
        }
    }

    internal static class CompressorExtensions
    {
        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string Decompress(this string compressedText)
        {
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }
    }

    internal static class JsonUtilityHelper
    {
        public static bool IsJsonArray(string json)
        {
            return json.StartsWith("[") && json.EndsWith("]");
        }

        public static T[] FromJson<T>(string json)
        {
            if (!IsJsonArray(json))
            {
                throw new System.FormatException("The input json string is not a Json Array");
            }
            json = "{\"Items\":" + json + "}";
            JsonWrapper<T> wrapper = JsonUtility.FromJson<JsonWrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            JsonWrapper<T> wrapper = new JsonWrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            JsonWrapper<T> wrapper = new JsonWrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class JsonWrapper<T>
        {
            public T[] Items;
        }
    }
}
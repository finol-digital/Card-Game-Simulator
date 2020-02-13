#if !UNITY_WSA || UNITY_EDITOR
using UnityEngine;

namespace Crosstales.Common.Util
{
   /// <summary>Serialize and deserialize objects to/from binary files.</summary>
   //public partial class SerializeDeSerialize<T>
   public static class SerializeDeSerialize
   {
      private static System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter
      {
         get
         {
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            bf.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

            return bf;
         }
      }

      /// <summary>Serialize an object to a byte-array.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <param name="filename">Binary-file for the object</param>
      /// <returns>Byte-array of the object</returns>
      public static void SerializeToFile<T>(T obj, string filename)
      {
         try
         {
            using (System.IO.FileStream fileStream = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
               binaryFormatter.Serialize(fileStream, obj);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not serialize the object to a file: " + ex);
         }
      }

      /*
      public System.IO.MemoryStream SerializeToMemory(T o)
      {
          try {
              System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();

              binaryFormatter.Serialize(memoryStream, o);

              return memoryStream;
          }
          catch (System.Exception ex) 
          {
                  Debug.LogError("Could not serialize the object to a file: " + ex);
          }
          
          return null;
      }
      */

      /// <summary>Serialize an object to a byte-array.</summary>
      /// <param name="obj">Object to serialize.</param>
      /// <returns>Byte-array of the object</returns>
      public static byte[] SerializeToByteArray<T>(T obj)
      {
         byte[] arr = null;

         try
         {
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
               binaryFormatter.Serialize(memoryStream, obj);
               arr = memoryStream.ToArray();
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not serialize the object to a byte-array: " + ex);
         }

         return arr;
      }


      /// <summary>Deserialize a binary-file to an object.</summary>
      /// <param name="filename">Binary-file of the object</param>
      /// <returns>Object</returns>
      public static T DeserializeFromFile<T>(string filename)
      {
         try
         {
            using (System.IO.FileStream fileStream = new System.IO.FileStream(filename, System.IO.FileMode.Open))
            {
               return (T)binaryFormatter.Deserialize(fileStream);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not deserialize the object from a file: " + ex);
         }

         return default(T);
      }

      /// <summary>Deserialize a byte-array to an object.</summary>
      /// <param name="data">Byte-array of the object</param>
      /// <returns>Object</returns>
      public static T DeserializeFromByteArray<T>(byte[] data)
      {
         try
         {
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream(data))
            {
               return (T)binaryFormatter.Deserialize(memoryStream);
            }
         }
         catch (System.Exception ex)
         {
            Debug.LogError("Could not deserialize the object from a byte-array: " + ex);
         }

         return default(T);
      }
   }
}
#endif
// © 2017-2020 crosstales LLC (https://www.crosstales.com)
#if !UNITY_WSA || UNITY_EDITOR
namespace Crosstales.Common.Util
{
   /// <summary>Serializable Dictionary-class for XML.</summary>
   [System.Serializable]
   public class SerializableDictionary<TKey, TVal> : System.Collections.Generic.Dictionary<TKey, TVal>, System.Xml.Serialization.IXmlSerializable, System.Runtime.Serialization.ISerializable
   {
      #region Variables

      //private const string DictionaryNodeName = "Dictionary";
      private const string ItemNodeName = "Item";
      private const string KeyNodeName = "Key";
      private const string ValueNodeName = "Value";

      private System.Xml.Serialization.XmlSerializer keySerializer = null;
      private System.Xml.Serialization.XmlSerializer valueSerializer = null;

      #endregion


      #region Constructors

      public SerializableDictionary()
      {
         //empty
      }

      public SerializableDictionary(System.Collections.Generic.IDictionary<TKey, TVal> dictionary) : base(dictionary)
      {
         //empty
      }

      public SerializableDictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) : base(comparer)
      {
         //empty
      }

      public SerializableDictionary(int capacity) : base(capacity)
      {
         //empty
      }

      public SerializableDictionary(System.Collections.Generic.IDictionary<TKey, TVal> dictionary, System.Collections.Generic.IEqualityComparer<TKey> comparer) : base(dictionary, comparer)
      {
         //empty
      }

      public SerializableDictionary(int capacity, System.Collections.Generic.IEqualityComparer<TKey> comparer) : base(capacity, comparer)
      {
         //empty
      }

      #endregion


      #region ISerializable Members

      protected SerializableDictionary(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
      {
         int itemCount = info.GetInt32("ItemCount");
         for (int ii = 0; ii < itemCount; ii++)
         {
            System.Collections.Generic.KeyValuePair<TKey, TVal> kvp = (System.Collections.Generic.KeyValuePair<TKey, TVal>)info.GetValue(string.Format("Item{0}", ii), typeof(System.Collections.Generic.KeyValuePair<TKey, TVal>));
            Add(kvp.Key, kvp.Value);
         }
      }

      void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
      {
         info.AddValue("ItemCount", Count);
         int itemIdx = 0;
         foreach (System.Collections.Generic.KeyValuePair<TKey, TVal> kvp in this)
         {
            info.AddValue(string.Format("Item{0}", itemIdx), kvp, typeof(System.Collections.Generic.KeyValuePair<TKey, TVal>));
            itemIdx++;
         }
      }

      #endregion


      #region IXmlSerializable Members

      void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
      {
         foreach (System.Collections.Generic.KeyValuePair<TKey, TVal> kvp in this)
         {
            writer.WriteStartElement(ItemNodeName);
            writer.WriteStartElement(KeyNodeName);
            KeySerializer.Serialize(writer, kvp.Key);
            writer.WriteEndElement();
            writer.WriteStartElement(ValueNodeName);
            ValueSerializer.Serialize(writer, kvp.Value);
            writer.WriteEndElement();
            writer.WriteEndElement();
         }
      }

      void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
      {
         if (reader.IsEmptyElement)
         {
            return;
         }

         if (!reader.Read())
         {
            throw new System.Xml.XmlException("Error in Deserialization of Dictionary");
         }

         while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
         {
            reader.ReadStartElement(ItemNodeName);
            reader.ReadStartElement(KeyNodeName);
            TKey key = (TKey)KeySerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadStartElement(ValueNodeName);
            TVal value = (TVal)ValueSerializer.Deserialize(reader);
            reader.ReadEndElement();
            reader.ReadEndElement();
            Add(key, value);
            reader.MoveToContent();
         }

         reader.ReadEndElement();
      }

      System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
      {
         return null;
      }

      #endregion


      #region Private Properties

      private System.Xml.Serialization.XmlSerializer ValueSerializer
      {
         get { return valueSerializer ?? (valueSerializer = new System.Xml.Serialization.XmlSerializer(typeof(TVal))); }
      }

      private System.Xml.Serialization.XmlSerializer KeySerializer
      {
         get { return keySerializer ?? (keySerializer = new System.Xml.Serialization.XmlSerializer(typeof(TKey))); }
      }

      #endregion
   }
}
#endif
// © 2014-2020 crosstales LLC (https://www.crosstales.com)
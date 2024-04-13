using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

namespace Metatron.Core
{
    [XmlRoot("dictionary")]
    //Inherit from both Dictionary<> and IXmlSerializable so we have access to everything
    //a dictionary has, as well as everything needed for serialization
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        #region IXmlSerializable Members
        //We won't be using GetSchema, but we have to implement it for the IXmlSerializable interface.
        public XmlSchema GetSchema()
        {
            return null;
        }

        //This is where we handle the bulk of the deserialization. Essentially, since the keys and pairs in
        // a dictionary are paired 1:1, we can serialize the list of keys and list of values separately, and pair
        // them up upon deserialization. Why MS didn't think of this, I will never know.
        public void ReadXml(XmlReader reader)
        {
            //Serializers for the keys and values
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            //Make sure we have something to deserialize
            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            //Deserialize items until we have no more to deserialize (until we run out of elements)
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");    //Go to a serialized item
                reader.ReadStartElement("key");     //Go to the key
				TKey key = default(TKey);
				try
				{
					key = (TKey)keySerializer.Deserialize(reader); //Deserialize our item "key"
				}
				catch (InvalidCastException)
				{

				}
                reader.ReadEndElement();            //End the key read
                reader.ReadStartElement("value");   //Go to the value
				TValue value = default(TValue);
				try
				{
					value = (TValue)valueSerializer.Deserialize(reader); //Deserialize our item "value"
				}
				catch (InvalidCastException)
				{

				}
                reader.ReadEndElement();            //End the value read
                this.Add(key, value);               //Add the newly-read key-value pair to our dictionary
                reader.ReadEndElement();            //End the item read
                reader.MoveToContent();             //Try to move to the next element
            }
            reader.ReadEndElement();
        }

        //This is where the bulk of the serialization takes place. Essentially we iterate all
        // key/value pairs and write a key and its value in their own "item".
        public void WriteXml(XmlWriter writer)
        {
            //Serializers
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

            //Loop all items in the dictionary
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");       //Write the start of an item
                writer.WriteStartElement("key");        //Write the start of a key element
				try
				{
					keySerializer.Serialize(writer, key);   //Serialize our "key" in the key elemnt
				}
				catch (InvalidOperationException) { }
                writer.WriteEndElement();               //End the key element
                writer.WriteStartElement("value");      //Write the start of a value element
                TValue value = this[key];
				try
				{
					valueSerializer.Serialize(writer, value);   //Serialize our "value" in the value element
				}
				catch (InvalidOperationException) { }
                writer.WriteEndElement();               //Write the end of our value element
                writer.WriteEndElement();               //Write the end of our item
            }
        }

        #endregion
    }
}


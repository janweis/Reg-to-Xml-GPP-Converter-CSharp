using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Reg_To_XmlGpp
{
    internal class XmlHandler
    {
        public WriteState WriterStatus { get; set; }
        private XmlWriter xmlWriter { get; set; }
        private ItemAction Action { get; set; }

        public XmlHandler(string filePath, ItemAction action)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.Unicode,
                OmitXmlDeclaration = true
            };

            this.Action = action;
            this.xmlWriter = XmlWriter.Create(filePath, xmlWriterSettings);
            this.WriterStatus = this.xmlWriter.WriteState;

            PrepareXmlDocument();
        }


        //
        // FUNCTIONS
        //


        /// <summary>
        /// Startup Document
        /// </summary>
        private void PrepareXmlDocument()
        {
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("RegistrySettings");
            xmlWriter.WriteAttributeString("clsid", "{A3CCFC41-DFDB-43a5-8D26-0FE8B954DA51}");
        }


        /// <summary>
        /// Get 
        /// </summary>
        private int GetItemActionNumber(ItemType itemType)
        {
            if (itemType == ItemType.REG_SZ || itemType == ItemType.REG_EXPAND_SZ || itemType == ItemType.REG_MULTI_SZ)
            {
                // REG_SZ, REG_EXPAND_SZ, REG_MULTI_SZ
                switch (Action)
                {
                    case ItemAction.Create:
                        return 5;
                    case ItemAction.Update:
                        return 7;
                    case ItemAction.Replace:
                        return 6;
                    case ItemAction.Delete:
                        return 8;
                }
            }
            else
            {
                // REG_BINARY, REG_QWORD, REG_DWORD
                switch (Action)
                {
                    case ItemAction.Create:
                        return 10;
                    case ItemAction.Update:
                        return 15;
                    case ItemAction.Replace:
                        return 11;
                    case ItemAction.Delete:
                        return 13;
                }
            }

            return 0;
        }


        /// <summary>
        /// Open new Xml Entry
        /// </summary>
        public void NewXmlEntry(string hive, string key, string itemName, string itemValue, ItemType itemType, List<string> itemExtendedValues = null, bool isDefaultKeyItem = false)
        {
            // Process Default Entry
            string itemStatus = itemName;
            if (isDefaultKeyItem)
            {
                itemName = "Hive";
                itemStatus = "(Default)";
            }

            // Process Entry Values
            string isDefault = (isDefaultKeyItem ? 1 : 0).ToString();
            string image = GetItemActionNumber(itemType).ToString();

            // Set for Only Key
            string itemTypeValue = itemType.ToString(); // default
            if (itemType == ItemType.OnlyRegKey)
                itemTypeValue = ""; // only Key, no Data

            // Write Registry
            xmlWriter.WriteStartElement("Registry");
            xmlWriter.WriteAttributeString("clsid", "{9CD4B2F4-923D-47f5-A062-E897DD1DAD50}");
            xmlWriter.WriteAttributeString("name", itemName);
            xmlWriter.WriteAttributeString("status", itemStatus);
            xmlWriter.WriteAttributeString("image", image);
            xmlWriter.WriteAttributeString("descr", "Imported with RegToGppXML-Converter (it-explorations.de)");

            // Write Properties
            xmlWriter.WriteStartElement("Properties");
            xmlWriter.WriteAttributeString("action", Action.ToString());
            xmlWriter.WriteAttributeString("hive", hive);
            xmlWriter.WriteAttributeString("key", key);
            xmlWriter.WriteAttributeString("name", itemName);
            xmlWriter.WriteAttributeString("type", itemTypeValue);
            xmlWriter.WriteAttributeString("displayDecimal", "0");
            xmlWriter.WriteAttributeString("value", itemValue);
            xmlWriter.WriteAttributeString("default", isDefault);

            // Process Extended Values
            if (itemExtendedValues != null && itemExtendedValues.Count() > 0)
            {
                xmlWriter.WriteStartElement("Values");

                foreach (string value in itemExtendedValues)
                {
                    xmlWriter.WriteStartElement("Value");
                    xmlWriter.WriteString(value);
                    CloseXmlEntry(); // Close Value
                }

                CloseXmlEntry(); // Close Values
            }

            // Close
            CloseXmlEntry(); // Close Properties
            CloseXmlEntry(); // Close Registry
        }


        /// <summary>
        /// Close Xml Entry
        /// </summary>
        public void CloseXmlEntry()
        {
            xmlWriter.WriteEndElement();
            xmlWriter.Flush();
        }


        /// <summary>
        /// Open new Collection
        /// </summary>
        public void OpenXmlCollection(string name)
        {
            xmlWriter.WriteStartElement("Collection");
            xmlWriter.WriteAttributeString("clsid", "{53B533F5-224C-47e3-B01B-CA3B3F3FF4BF}");
            xmlWriter.WriteAttributeString("name", name);
        }


        /// <summary>
        /// Close Collection
        /// </summary>
        public void CloseXmlCollection()
            => CloseXmlEntry();


        /// <summary>
        /// Create empty Key
        /// </summary>
        public void CreateEmptyKey(string hive, string key)
            => NewXmlEntry(hive, key, "", "", ItemType.OnlyRegKey);


        /// <summary>
        /// Close XmlWriter
        /// </summary>
        public void CloseXml()
        {
            xmlWriter.WriteEndDocument();
            xmlWriter.Flush();
            xmlWriter.Close();
        }


        /// <summary>
        /// Get Writer
        /// </summary>
        public XmlWriter GetXmlWriter() { return xmlWriter; }

    }
}
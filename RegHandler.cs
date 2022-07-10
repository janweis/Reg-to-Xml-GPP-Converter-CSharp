using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Reg_To_XmlGpp
{
    internal class RegHandler
    {
        private string RegFilePath { get; set; }
        private string OutputFilePath { get; set; }
        private XmlHandler XmlHandler { get; set; }

        private string CurrentKey { get; set; }
        private string CurrentHive { get; set; }

        public RegHandler(string regFilePath, string outputFilePath, ItemAction itemAction)
        {
            RegFilePath = regFilePath;

            if (string.IsNullOrEmpty(outputFilePath))
                OutputFilePath = BuildOutputPath(regFilePath);
            else
                OutputFilePath = outputFilePath;

            XmlHandler = new XmlHandler(OutputFilePath, itemAction);
        }

        //
        // PROCESSING FUNCTIONS
        //

        /// <summary>
        /// Start 
        /// </summary>
        public void Start()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n---------------------------------------");
                Console.WriteLine(" Registry to Xml GPP Converter v0.1");
                Console.WriteLine(" Jan Weis, www.it-explorations.de");
                Console.WriteLine("---------------------------------------\n");

                Console.ResetColor();
                Console.WriteLine($"[info] Input File: {RegFilePath}");
                Console.WriteLine($"[info] Output File: {OutputFilePath}\n");

                Console.WriteLine("[*] Validating File...");
                ValidateRegFile();

                Console.WriteLine("[*] Processing Data...");
                ProcessRegFile();

                Console.WriteLine("[*] Finalizing...");
                Finalizing();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@"
 ----------
< Job done >
 ----------
         \   ^__^ 
          \  (oo)\_______
             (__)\       )\/\
                 ||----w |
                 ||     ||
");
            }
            catch (FileNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Error] File was not found! Pfad:{ex.Message}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Error] {ex.Message}");
                Console.ReadKey();
            }
        }


        /// <summary>
        /// Process RegistryFile
        /// </summary>
        private void ProcessRegFile()
        {
            StreamReader streamReader = new StreamReader(RegFilePath, Encoding.Unicode, true);
            string lastRegLine = string.Empty;
            bool lastLineWithData = false;

            while (streamReader.EndOfStream == false &&
                (XmlHandler.WriterStatus != WriteState.Closed && XmlHandler.WriterStatus != WriteState.Error) == true)
            {
                // Getting Data...
                string regLine = streamReader.ReadLine();

                // Remove Unicode Control Characters
                if (Regex.IsMatch(regLine, @"\p{C}+"))
                    regLine = Regex.Replace(regLine, @"\p{C}+", string.Empty);

                // Skip empty line
                if (string.IsNullOrEmpty(regLine) || regLine.Equals("Windows Registry Editor Version 5.00"))
                    continue;

                // Process Data
                if (regLine.StartsWith("["))
                {
                    // KEYs
                    ProcessKeyLine(regLine, lastRegLine, lastLineWithData);
                    lastLineWithData = false;
                    lastRegLine = regLine;
                }
                else if (regLine.StartsWith("\"") || regLine.StartsWith("@"))
                {
                    // DATA
                    try
                    {
                        ProcessDataLine(regLine, streamReader);
                        lastLineWithData = true;
                    }
                    catch (NotSupportedException ex)
                    {
                        Console.WriteLine($"[Not Supported] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Debug] LineData: {regLine}; Message: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine($"[!] Ignore undefined input: {regLine}");
                    continue;
                }
            }
        }


        /// <summary>
        /// Process Items
        /// </summary>
        private void ProcessDataLine(string line, StreamReader streamReader)
        {
            // INIT
            bool isDefaultItemName = false;

            // Get Item
            int equalSign;
            string itemName;
            if (line.StartsWith("@"))
            {
                equalSign = 1;
                isDefaultItemName = true;
                itemName = "@";
            }
            else
            {
                if (line.StartsWith("\""))
                {
                    equalSign = line.IndexOf("\"=") + 1;
                    itemName = line.Substring(1, equalSign - 2);
                }
                else
                {
                    return;
                }
            }

            // Get Type
            string typeChar = line.Substring(equalSign + 1, 1);
            int typeToValueSeparator;
            string itemTypeName;

            if (typeChar == "\"")
            {
                // SZ
                typeToValueSeparator = equalSign + 1;
                itemTypeName = "\"";
            }
            else if (typeChar == "d" || typeChar == "h")
            {
                // DWORD or HEX
                string tempLine;
                typeToValueSeparator = line.IndexOf(":");

                while (typeToValueSeparator < equalSign)
                {
                    tempLine = line.Substring(typeToValueSeparator + 1);
                    typeToValueSeparator = tempLine.IndexOf(":");
                }

                int itemTypeLength = typeToValueSeparator - equalSign;
                itemTypeName = line.Substring(equalSign + 1, itemTypeLength - 1);
            }
            else
                throw new Exception("Could not detect Registry Type of Item!");

            ItemType itemType = GetTypeByString(itemTypeName);


            // Get Value & Process Value
            string itemValue = line.Substring(typeToValueSeparator + 1);
            itemValue = Regex.Unescape(itemValue); // Remove Escapes

            // Process multiline values
            List<string> multiLineData = new List<string>();
            if (line.EndsWith("\\"))
            {
                multiLineData.Add(itemValue);
                if (streamReader.EndOfStream == false)
                {
                    string nextDataLine;
                    do
                    {
                        nextDataLine = streamReader.ReadLine().TrimStart();
                        if (nextDataLine.StartsWith("\"") == false)
                        {
                            multiLineData.Add(Regex.Unescape(nextDataLine));
                        }
                        else
                        {
                            // Whoops! Catch unexpected new Item.
                            ProcessDataLine(nextDataLine, streamReader);
                        }
                    } while (nextDataLine.EndsWith("\\") && streamReader.EndOfStream == false);
                }

                itemValue = string.Join("", multiLineData.ToArray());
            }

            // Process type
            if (itemType == ItemType.REG_SZ)
            {
                itemValue = itemValue.TrimEnd('\"'); // Remove Quotes
            }
            else if (itemType == ItemType.REG_BINARY)
            {
                itemValue = itemValue.Trim(',');
            }
            else if (itemType == ItemType.REG_EXPAND_SZ)
            {
                itemValue = Converter
                    .HexToString(itemValue)
                    .Replace("\0", string.Empty);
            }
            else if (itemType == ItemType.REG_QWORD)
            {
                List<string> strings = itemValue.Split(',').ToList();
                strings.Reverse();
                itemValue = String.Join("", strings.ToArray());
            }

            // Write Data
            XmlHandler.NewXmlEntry(CurrentHive, CurrentKey, itemName, itemValue, itemType, multiLineData, isDefaultItemName);
        }


        /// <summary>
        /// Process Keys
        /// </summary>
        private void ProcessKeyLine(string currentKey, string lastKey, bool lastLineWithData)
        {
            List<string> keyArray = currentKey
                .Replace("[", "")
                .Replace("]", "")
                .Split('\\')
                .ToList();

            List<string> lastkeyArray = new List<string>();
            if (lastKey != "")
                lastkeyArray = lastKey
                    .Replace("[", "")
                    .Replace("]", "")
                    .Split('\\')
                    .ToList();

            // Get Hive and Key
            CurrentHive = keyArray[0];
            CurrentKey = string.Empty; // Only Hive, No Key
            if (keyArray.Count > 1)
                CurrentKey = string.Join("\\", keyArray).Substring(CurrentHive.Length + 1);

            string lastHive = string.Empty;
            string lastKeyString = string.Empty; // Only Hive, No Key
            if (lastkeyArray.Count > 1)
            {
                lastHive = lastkeyArray[0];
                if (lastkeyArray.Count > 1)
                    lastKeyString = string.Join("\\", lastkeyArray).Substring(lastHive.Length + 1);
            }

            // Compare Keys
            var keysToOpen = keyArray.Except(lastkeyArray);
            var keysToClose = lastkeyArray.Except(keyArray);

            // Close old Keys
            if (keysToClose != null && keysToClose.Count() > 0)
            {
                for (int i = 0; i < keysToClose.Count(); i++)
                {
                    if (lastLineWithData == false)
                        if (lastHive != string.Empty && lastKeyString != string.Empty)
                            XmlHandler.CreateEmptyKey(lastHive, lastKeyString);

                    XmlHandler.CloseXmlEntry();
                }
            }

            // Open new Keys
            if (keysToOpen != null && keysToOpen.Count() > 0)
                foreach (string key in keysToOpen)
                    XmlHandler.OpenXmlCollection(key);
        }


        /// <summary>
        /// Finish Process
        /// </summary>
        private void Finalizing()
            => XmlHandler.CloseXml();


        //
        // HELPER FUNCTIONS
        //

        /// <summary>
        /// Build Outputpath, if no exists
        /// </summary>
        private string BuildOutputPath(string regFilePath)
        {
            FileInfo fileInfo = new FileInfo(regFilePath);
            string newFilePath = Path.ChangeExtension(fileInfo.FullName, ".xml");

            return newFilePath;
        }


        /// <summary>
        /// Validate RegFile Requisites
        /// </summary>
        private void ValidateRegFile()
        {
            if (File.Exists(RegFilePath) == false)
                throw new FileNotFoundException(RegFilePath);

            if (new FileInfo(RegFilePath).Extension != ".reg")
                throw new Exception("No '.reg' extension was found!");
        }


        /// <summary>
        /// Get Type by string reg-identifier
        /// </summary>
        private ItemType GetTypeByString(string value)
        {
            switch (value)
            {
                case "\"":
                    return ItemType.REG_SZ;
                case "dword":
                    return ItemType.REG_DWORD;
                case "hex(4)":
                    return ItemType.REG_DWORD;
                case "hex":
                    return ItemType.REG_BINARY;
                case "hex(3)":
                    return ItemType.REG_BINARY;
                case "hex(2)":
                    return ItemType.REG_EXPAND_SZ;
                case "hex(7)":
                    return ItemType.REG_MULTI_SZ;
                case "hex(b)":
                    return ItemType.REG_QWORD;
                case "hex(5)":
                    throw new NotSupportedException($"REG_DWORD_BIG_ENDIAN is not supported!");
                case "hex(6)":
                    throw new NotSupportedException($"REG_LINK is not supported!");
                case "hex(0)":
                    throw new NotSupportedException($"REG_NONE is not supported!");
                default:
                    throw new NotSupportedException($"Unknown Type: {value}!");
            }
        }
    }
}

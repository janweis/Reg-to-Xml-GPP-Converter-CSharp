using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Reg_To_XmlGpp
{
    internal class Converter
    {
        public static string HexToString(string hexData)
        {
            List<byte> byteList = new List<byte>();
            foreach (string s in hexData.Split(','))
                byteList.Add(byte.Parse(s, NumberStyles.HexNumber));

            return Encoding.Unicode.GetString(byteList.ToArray());
        }
    }
}

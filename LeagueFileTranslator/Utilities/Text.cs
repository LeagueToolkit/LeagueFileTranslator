using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Utilities
{
    public static class Text
    {
        public static string ReadPaddedString(BinaryReader br, int length)
        {
            return Encoding.ASCII.GetString(br.ReadBytes(length).TakeWhile(b => !b.Equals(0)).ToArray());
        }
        
        public static string ReadZeroTerminatedString(BinaryReader br)
        {
            string returnString = "";

            while (true)
            {
                char c = br.ReadChar();
                if (c == 0)
                {
                    break;
                }

                returnString += c;
            }

            return returnString;
        }
    }
}

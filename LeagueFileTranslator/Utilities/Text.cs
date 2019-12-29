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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Structures
{
    public class ColorRGBA4B
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        public ColorRGBA4B(BinaryReader br)
        {
            this.R = br.ReadByte();
            this.G = br.ReadByte();
            this.B = br.ReadByte();
            this.A = br.ReadByte();
        }
    }
}

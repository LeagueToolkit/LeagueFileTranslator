using System;
using System.Globalization;
using System.IO;

namespace LeagueFileTranslator.Structures
{
    public class ColorRGBA4B
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        /// <summary>
        /// Initializes a new <see cref="ColorRGBAVector4Byte"/>
        /// </summary>
        public ColorRGBA4B(byte r, byte g, byte b, byte a)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        /// <summary>
        /// Initializes a new <see cref="ColorRGBAVector4Byte"/> from a <see cref="BinaryReader"/>
        /// </summary>
        /// <param name="br">The <see cref="BinaryReader"/> to read from</param>
        public ColorRGBA4B(BinaryReader br)
        {
            this.R = br.ReadByte();
            this.G = br.ReadByte();
            this.B = br.ReadByte();
            this.A = br.ReadByte();
        }

        public ColorRGBA4B(StreamReader sr)
        {
            string[] input = sr.ReadLine().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            this.R = byte.Parse(input[0], CultureInfo.InvariantCulture.NumberFormat);
            this.G = byte.Parse(input[1], CultureInfo.InvariantCulture.NumberFormat);
            this.B = byte.Parse(input[2], CultureInfo.InvariantCulture.NumberFormat);
            this.A = byte.Parse(input[3], CultureInfo.InvariantCulture.NumberFormat);
        }

        /// <summary>
        /// Writes this <see cref="ColorRGBAVector4Byte"/> into a <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="bw">The <see cref="BinaryWriter"/> to write to</param>
        public void Write(BinaryWriter bw)
        {
            bw.Write(this.R);
            bw.Write(this.G);
            bw.Write(this.B);
            bw.Write(this.A);
        }

        /// <summary>
        /// Writes this <see cref="ColorRGBAVector4Byte"/> into a <see cref="StreamWriter"/> using the specified format
        /// </summary>
        /// <param name="sw">The <see cref="StreamWriter"/> to write to</param>
        /// <param name="format">Format that should be used for writing</param>
        public void Write(StreamWriter sw, bool newLine = false, string format = "{0} {1} {2} {3}")
        {
            if (newLine)
            {
                format += '\n';
            }

            sw.Write(string.Format(format, this.R, this.G, this.B, this.A));
        }

        public bool Equals(ColorRGBA4B other)
        {
            return this.R == other.R && this.G == other.G && this.B == other.B && this.A == other.A;
        }
    }
}

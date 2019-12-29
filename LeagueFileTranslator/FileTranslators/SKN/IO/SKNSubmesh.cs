using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.FileTranslators.SKN.IO
{
    public class SKNSubmesh
    {
        public string Name { get; private set; }
        public uint StartVertex { get; private set; }
        public uint VertexCount { get; private set; }
        public uint StartIndex { get; private set; }
        public uint IndexCount { get; private set; }

        public SKNSubmesh(BinaryReader br)
        {
            this.Name = Encoding.ASCII.GetString(br.ReadBytes(64)).Replace("\0", "");
            this.StartVertex = br.ReadUInt32();
            this.VertexCount = br.ReadUInt32();
            this.StartIndex = br.ReadUInt32();
            this.IndexCount = br.ReadUInt32();
        }
    }
}

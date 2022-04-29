using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueFileTranslator.Utilities;

namespace LeagueFileTranslator.FileTranslators.SKN.IO
{
    public class SKNSubmesh
    {
        public string Name { get; private set; }
        public uint StartVertex { get; private set; }
        public uint VertexCount { get; private set; }
        public uint StartIndex { get; private set; }
        public uint IndexCount { get; private set; }

        public SKNSubmesh(string name, uint startVertex, uint vertexCount, uint startIndex, uint indexCount)
        {
            this.Name = name;
            this.StartVertex = startVertex;
            this.VertexCount = vertexCount;
            this.StartIndex = startIndex;
            this.IndexCount = indexCount;
        }

        public SKNSubmesh(BinaryReader br)
        {
            this.Name = Text.ReadPaddedString(br, 64);
            this.StartVertex = br.ReadUInt32();
            this.VertexCount = br.ReadUInt32();
            this.StartIndex = br.ReadUInt32();
            this.IndexCount = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(this.Name.PadRight(64, '\u0000').ToCharArray());
            bw.Write(this.StartVertex);
            bw.Write(this.VertexCount);
            bw.Write(this.StartIndex);
            bw.Write(this.IndexCount);
        }
    }
}

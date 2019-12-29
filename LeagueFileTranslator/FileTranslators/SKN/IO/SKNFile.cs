using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Structures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.FileTranslators.SKN.IO
{
    public class SKNFile
    {
        public List<SKNSubmesh> Submeshes = new List<SKNSubmesh>();
        public List<ushort> Indices = new List<ushort>();
        public List<SKNVertex> Vertices = new List<SKNVertex>();

        public SKNFile(string fileLocation) : this(File.OpenRead(fileLocation))
        {

        }

        public SKNFile(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                uint magic = br.ReadUInt32();
                if (magic != 0x00112233)
                {
                    throw new Exception("SKNFile.Read: Not a valid SKN file");
                }

                ushort major = br.ReadUInt16();
                ushort minor = br.ReadUInt16();
                if (major != 2 && major != 4 && minor != 1)
                {
                    throw new Exception("SKNFile.Read: Unsupported SKN version: " + major + "." + minor);
                }

                uint submeshCount = br.ReadUInt32();
                for (int i = 0; i < submeshCount; i++)
                {
                    this.Submeshes.Add(new SKNSubmesh(br));
                }

                uint flags = 0;
                if (major == 4)
                {
                    flags = br.ReadUInt32();
                }

                uint indexCount = br.ReadUInt32();
                uint vertexCount = br.ReadUInt32();

                MGlobal.displayInfo("vcount: " + vertexCount);
                MGlobal.displayInfo("icount: " + indexCount);

                uint vertexSize = 52;
                SKNVertexType vertexType = SKNVertexType.Basic;
                R3DBox boundingBox = null;
                R3DSphere boundingSphere = null;
                if (major == 4)
                {
                    vertexSize = br.ReadUInt32();
                    vertexType = (SKNVertexType)br.ReadUInt32();
                    boundingBox = new R3DBox(br);
                    boundingSphere = new R3DSphere(br);
                }


                for (int i = 0; i < indexCount; i++)
                {
                    this.Indices.Add(br.ReadUInt16());
                }

                for (int i = 0; i < vertexCount; i++)
                {
                    this.Vertices.Add(new SKNVertex(br, vertexType));
                }
            }
        }
    }
}

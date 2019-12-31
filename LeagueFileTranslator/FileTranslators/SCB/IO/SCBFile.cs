using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueFileTranslator.Structures;
using Autodesk.Maya.OpenMaya;

namespace LeagueFileTranslator.FileTranslators.SCB.IO
{
    public class SCBFile
    {
        public string Name { get; set; }
        public R3DBox BoundingBox { get; private set; }
        public Vector3 CentralPoint { get; private set; }
        public List<Vector3> Vertices { get; private set; } = new List<Vector3>();
        public List<Vector4b> Tangents { get; private set; } = new List<Vector4b>();
        public Dictionary<string, List<SCBFace>> Materials { get; private set; } = new Dictionary<string, List<SCBFace>>();
        public List<Vector3b> VertexColors { get; private set; } = new List<Vector3b>();

        public SCBFile(List<Vector3> vertices, Dictionary<string, List<SCBFace>> materials)
        {
            this.Vertices = vertices;
            this.Materials = materials;
        }
        public SCBFile(List<Vector3> vertices, List<uint> indices, List<Vector2> uvs)
        {
            this.Vertices = vertices;

            List<SCBFace> faces = new List<SCBFace>();
            for (int i = 0; i < indices.Count; i += 3)
            {
                faces.Add(new SCBFace(new uint[] { indices[i], indices[i + 1], indices[i + 2] }, "lambert1", new Vector2[] { uvs[i], uvs[i + 1], uvs[i + 2] }));
            }
            this.Materials.Add("lambert1", faces);
        }

        public SCBFile(string fileLocation) : this(File.OpenRead(fileLocation)) { }
        public SCBFile(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                string magic = Encoding.ASCII.GetString(br.ReadBytes(8));
                if (magic != "r3d2Mesh")
                {
                    throw new Exception("This is not a valid SCB file");
                }

                ushort major = br.ReadUInt16();
                ushort minor = br.ReadUInt16();
                if (major != 3 && major != 2 && minor != 1) //There are versions [2][1] and [1][1] aswell
                {
                    throw new Exception(string.Format("The Version: {0}.{1} is not supported", major, minor));
                }

                this.Name = Encoding.ASCII.GetString(br.ReadBytes(128)).Replace("\0", "");
                uint vertexCount = br.ReadUInt32();
                uint faceCount = br.ReadUInt32();
                SCBFlags flags = (SCBFlags)br.ReadUInt32();
                this.BoundingBox = new R3DBox(br);

                bool hasTangents = false;
                if (major == 3 && minor == 2)
                {
                    hasTangents = br.ReadUInt32() == 1;
                }

                for (int i = 0; i < vertexCount; i++)
                {
                    this.Vertices.Add(new Vector3(br));
                }

                if (major == 3 && minor == 2 && flags.HasFlag(SCBFlags.TANGENTS) && hasTangents)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        this.Tangents.Add(new Vector4b(br));
                    }
                }

                this.CentralPoint = new Vector3(br);

                for (int i = 0; i < faceCount; i++)
                {
                    SCBFace face = new SCBFace(br);

                    if (!this.Materials.ContainsKey(face.Material))
                    {
                        this.Materials.Add(face.Material, new List<SCBFace>());
                    }

                    this.Materials[face.Material].Add(face);
                }

                if (flags.HasFlag(SCBFlags.VERTEX_COLORS))
                {
                    this.VertexColors.Add(new Vector3b(br));
                }
            }

            PrintInfo();
        }

        public void PrintInfo()
        {
            MGlobal.displayInfo("SCBFile - " + this.Name);
            MGlobal.displayInfo("SCBFile - Vertex Count: " + this.Vertices.Count);

            foreach(KeyValuePair<string, List<SCBFace>> material in this.Materials)
            {
                MGlobal.displayInfo(string.Format("SCBFile - Material: {0} Face Count: {1}", material.Key, material.Value.Count));
            }
        }

        public void Load(string name)
        {
            MFnMesh mesh = new MFnMesh();
            MDagPath meshDagPath = mesh.dagPath;

            mesh.setName(name);
        }

        public void Write(string fileLocation)
        {
            Write(File.Create(fileLocation));
        }
        public void Write(Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(Encoding.ASCII.GetBytes("r3d2Mesh"));
                bw.Write((ushort)3);
                bw.Write((ushort)2);
                bw.Write(this.Name.PadRight(128, '\u0000').ToCharArray());
                bw.Write((uint)this.Vertices.Count);

                uint faceCount = 0;
                foreach (KeyValuePair<string, List<SCBFace>> material in this.Materials)
                {
                    faceCount += (uint)material.Value.Count;
                }
                bw.Write(faceCount);

                SCBFlags flags = 0;
                uint hasTangents = 0;
                if (this.Tangents.Count != 0)
                {
                    flags |= SCBFlags.TANGENTS;
                    hasTangents = 1;
                }
                if (this.VertexColors.Count != 0)
                {
                    flags |= SCBFlags.VERTEX_COLORS;
                }
                bw.Write((uint)flags);
                CalculateBoundingBox().Write(bw);
                bw.Write(hasTangents);

                foreach (Vector3 vertex in this.Vertices)
                {
                    vertex.Write(bw);
                }
                foreach (Vector4b tangent in this.Tangents)
                {
                    tangent.Write(bw);
                }
                CalculateCentralPoint().Write(bw);
                foreach (KeyValuePair<string, List<SCBFace>> material in this.Materials)
                {
                    foreach (SCBFace face in material.Value)
                    {
                        face.Write(bw);
                    }
                }
                foreach (Vector3b color in this.VertexColors)
                {
                    color.Write(bw);
                }
            }
        }

        public R3DBox CalculateBoundingBox()
        {
            Vector3 min = this.Vertices[0];
            Vector3 max = this.Vertices[0];

            foreach (Vector3 vertex in this.Vertices)
            {
                if (min.X > vertex.X) min.X = vertex.X;
                if (min.Y > vertex.Y) min.Y = vertex.Y;
                if (min.Z > vertex.Z) min.Z = vertex.Z;
                if (max.X < vertex.X) max.X = vertex.X;
                if (max.Y < vertex.Y) max.Y = vertex.Y;
                if (max.Z < vertex.Z) max.Z = vertex.Z;
            }

            return new R3DBox(min, max);
        }
        public Vector3 CalculateCentralPoint()
        {
            R3DBox boundingBox = CalculateBoundingBox();
            return new Vector3(
                0.5f * (boundingBox.Max.X + boundingBox.Min.X),
                0.5f * (boundingBox.Max.Y + boundingBox.Min.Y),
                0.5f * (boundingBox.Max.Z + boundingBox.Min.Z)
                );
        }
        public Vector3 CalculateCentralPoint(R3DBox boundingBox)
        {
            return new Vector3(
                0.5f * (boundingBox.Max.X + boundingBox.Min.X),
                0.5f * (boundingBox.Max.Y + boundingBox.Min.Y),
                0.5f * (boundingBox.Max.Z + boundingBox.Min.Z)
                );
        }
    }

    [Flags]
    public enum SCBFlags : uint
    {
        VERTEX_COLORS = 1,
        TANGENTS = 2
    }
}

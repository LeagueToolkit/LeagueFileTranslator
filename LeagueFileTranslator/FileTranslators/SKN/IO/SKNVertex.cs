using LeagueFileTranslator.Structures;
using System.IO;

namespace LeagueFileTranslator.FileTranslators.SKN.IO
{
    public class SKNVertex
    {
        public Vector3 Position { get; set; }
        public byte[] BoneIndices { get; set; } = new byte[4];
        public float[] Weights { get; set; } = new float[4];
        public Vector3 Normal { get; set; }
        public Vector2 UV { get; set; }
        public ColorRGBA4B Color { get; set; }

        public int UVIndex { get; set; }
        public int DataIndex { get; set; }

        public SKNVertex(Vector3 position, byte[] boneIndices, float[] weights, Vector3 normal, Vector2 uv)
        {
            this.Position = position;
            this.BoneIndices = boneIndices;
            this.Weights = weights;
            this.Normal = normal;
            this.UV = uv;
        }

        public SKNVertex(BinaryReader br, SKNVertexType type)
        {
            this.Position = new Vector3(br);

            for (int i = 0; i < 4; i++)
            {
                this.BoneIndices[i] = br.ReadByte();
            }

            for (int i = 0; i < 4; i++)
            {
                this.Weights[i] = br.ReadSingle();
            }

            this.Normal = new Vector3(br);
            this.UV = new Vector2(br);

            if (type == SKNVertexType.Color)
            {
                this.Color = new ColorRGBA4B(br);
            }
        }

        public void Write(BinaryWriter bw)
        {
            this.Position.Write(bw);

            for (int i = 0; i < 4; i++)
            {
                bw.Write(this.BoneIndices[i]);
            }

            for (int i = 0; i < 4; i++)
            {
                bw.Write(this.Weights[i]);
            }

            this.Normal.Write(bw);
            this.UV.Write(bw);
        }
    }

    public enum SKNVertexType : uint
    {
        Basic = 0,
        Color = 1
    }
}

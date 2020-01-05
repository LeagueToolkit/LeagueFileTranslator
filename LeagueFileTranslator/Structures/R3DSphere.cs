using System.IO;

namespace LeagueFileTranslator.Structures
{
    public class R3DSphere
    {
        public Vector3 Position { get; set; }
        public float Radius { get; set; }

        public R3DSphere(Vector3 position, float radius)
        {
            this.Position = position;
            this.Radius = radius;
        }

        public R3DSphere(BinaryReader br)
        {
            this.Position = new Vector3(br);
            this.Radius = br.ReadSingle();
        }

        public void Write(BinaryWriter bw)
        {
            this.Position.Write(bw);
            bw.Write(this.Radius);
        }
    }
}

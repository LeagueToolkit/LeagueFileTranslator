using System;
using System.IO;

namespace LeagueFileTranslator.Structures
{
    public class Quaternion
    {
        /// <summary>
        /// The X component
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// The Y component
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// The Z component
        /// </summary>
        public float Z { get; set; }
        /// <summary>
        /// The W component
        /// </summary>
        public float W { get; set; }

        /// <summary>
        /// Initializes a new <see cref="Quaternion"/> instance
        /// </summary>
        public Quaternion()
        {

        }

        public Quaternion(BinaryReader br)
        {
            this.X = br.ReadSingle();
            this.Y = br.ReadSingle();
            this.Z = br.ReadSingle();
            this.W = br.ReadSingle();
        }

        /// <summary>
        /// Initializes a new <see cref="Quaternion"/> instance
        /// </summary>
        public Quaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        /// <summary>
        /// Creates a clone of a <see cref="Quaternion"/> object
        /// </summary>
        /// <param name="quaternion">The <see cref="Quaternion"/> to clone</param>
        public Quaternion(Quaternion quaternion)
        {
            this.X = quaternion.X;
            this.Y = quaternion.Y;
            this.Z = quaternion.Z;
            this.W = quaternion.W;
        }

        /// <summary>
        /// Returns a normalized <see cref="Quaternion"/>
        /// </summary>
        public Quaternion Normalize()
        {
            float magnitude = (float)Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z + this.W * this.W);

            return new Quaternion()
            {
                X = this.X / magnitude,
                Y = this.Y / magnitude,
                Z = this.Z / magnitude,
                W = this.W / magnitude
            };
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion()
            {
                X = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z,
                Y = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                Z = a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                W = a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W
            };
        }
    }
}

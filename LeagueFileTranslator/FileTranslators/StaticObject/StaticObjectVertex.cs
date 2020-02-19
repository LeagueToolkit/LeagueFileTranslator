using LeagueFileTranslator.Structures;

namespace LeagueFileTranslator.FileTranslators.StaticObject
{
    public class StaticObjectVertex
    {
        public Vector3 Position { get; set; }
        public Vector2 UV { get; set; }
        public ColorRGBA4B Color { get; set; }

        public StaticObjectVertex(Vector3 position, Vector2 uv)
        {
            this.Position = position;
            this.UV = uv;
        }

        public StaticObjectVertex(Vector3 position, Vector2 uv, ColorRGBA4B color) : this(position, uv)
        {
            this.Color = color;
        }
    }
}

using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Structures;

namespace LeagueFileTranslator.Extensions
{
    public static class MFnMeshExtensions
    {
        public static bool ContainsHoles(this MFnMesh mesh)
        {
            MIntArray holeInfoArray = new MIntArray();
            MIntArray holeVertexArray = new MIntArray();

            mesh.getHoles(holeInfoArray, holeVertexArray);

            return holeInfoArray.length != 0;
        }
        public static bool IsTriangulated(this MFnMesh mesh)
        {
            MItMeshPolygon polygonIterator = new MItMeshPolygon(mesh.dagPath);

            for (polygonIterator.reset(); !polygonIterator.isDone; polygonIterator.next())
            {
                if (!polygonIterator.hasValidTriangulation)
                {
                    return false;
                }
            }

            return true;
        }

        public static MayaMeshData GetMeshData(this MFnMesh mesh) => new MayaMeshData(mesh);
    }
}

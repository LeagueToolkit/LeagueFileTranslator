using Autodesk.Maya.OpenMaya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Structures
{
    public class MayaMeshData
    {
        public MFnMesh Mesh { get; private set; }
        public uint Instance { get; private set; }
        public MObjectArray Shaders { get; private set; } = new MObjectArray();
        public MIntArray ShaderIndices { get; private set; } = new MIntArray();
        public MFloatPointArray VertexArray { get; private set; } = new MFloatPointArray();
        public MFloatArray UArray { get; private set; } = new MFloatArray();
        public MFloatArray VArray { get; private set; } = new MFloatArray();
        public MIntArray UVCounts { get; private set; } = new MIntArray();
        public MIntArray UVIds { get; private set; } = new MIntArray();
        public MIntArray TriangleCounts { get; private set; } = new MIntArray();
        public MIntArray TriangleVertices { get; private set; } = new MIntArray();
        public MFloatVectorArray Normals { get; private set; } = new MFloatVectorArray();

        public MayaMeshData(MFnMesh mesh)
        {
            this.Mesh = mesh;
            this.Instance = mesh.dagPath.isInstanced ? mesh.dagPath.instanceNumber : 0;

            mesh.getConnectedShaders(this.Instance, this.Shaders, this.ShaderIndices);
            mesh.getPoints(this.VertexArray, MSpace.Space.kWorld);
            mesh.getUVs(this.UArray, this.VArray);
            mesh.getAssignedUVs(this.UVCounts, this.UVIds);
            mesh.getTriangles(this.TriangleCounts, this.TriangleVertices);
            mesh.getVertexNormals(false, this.Normals);
        }
    }
}

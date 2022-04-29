using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueFileTranslator.Structures;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Helpers;
using LeagueFileTranslator.Extensions;
using LeagueFileTranslator.Utilities;

namespace LeagueFileTranslator.FileTranslators.StaticObject
{
    public class StaticObject
    {
        public string Name { get; set; }
        public Vector3 PivotPoint { get; set; }
        public List<StaticObjectSubmesh> Submeshes { get; private set; }

        public StaticObject(List<StaticObjectSubmesh> submeshes) : this(string.Empty, submeshes) { }
        public StaticObject(string name, List<StaticObjectSubmesh> submeshes)
        {
            this.Name = name;
            this.Submeshes = submeshes;
        }
        public StaticObject(string name, List<StaticObjectSubmesh> submeshes, Vector3 pivotPoint) : this(name, submeshes)
        {
            this.PivotPoint = pivotPoint;
        }

        public static StaticObject ReadSCB(string fileLocation) => ReadSCB(File.OpenRead(fileLocation));
        public static StaticObject ReadSCB(Stream stream)
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
                if (major != 3 && major != 2 && minor != 1)
                {
                    throw new Exception(string.Format("The Version: {0}.{1} is not supported", major, minor));
                }

                string name = Text.ReadPaddedString(br, 128);
                uint vertexCount = br.ReadUInt32();
                uint faceCount = br.ReadUInt32();
                StaticObjectFlags flags = (StaticObjectFlags)br.ReadUInt32();
                R3DBox boundingBox = new R3DBox(br);

                bool hasVertexColors = false;
                if (major == 3 && minor == 2)
                {
                    hasVertexColors = br.ReadUInt32() == 1;
                }

                List<Vector3> vertices = new List<Vector3>((int)vertexCount);
                List<ColorRGBA4B> vertexColors = new List<ColorRGBA4B>((int)vertexCount);
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices.Add(new Vector3(br));
                }

                if (hasVertexColors)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        vertexColors.Add(new ColorRGBA4B(br));
                    }
                }

                Vector3 centralPoint = new Vector3(br);

                List<StaticObjectFace> faces = new List<StaticObjectFace>((int)faceCount);
                for (int i = 0; i < faceCount; i++)
                {
                    faces.Add(new StaticObjectFace(br));
                }

                return new StaticObject(name, CreateSubmeshes(vertices, vertexColors, faces), centralPoint);
            }
        }

        public static StaticObject ReadSCO(string fileLocation) => ReadSCO(File.OpenRead(fileLocation));
        public static StaticObject ReadSCO(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                char[] splittingArray = new char[] { ' ' };
                string[] input = null;

                if (sr.ReadLine() != "[ObjectBegin]")
                {
                    throw new Exception("Invalid signature");
                }

                input = sr.ReadLine().Split(splittingArray, StringSplitOptions.RemoveEmptyEntries);
                string name = input.Length != 1 ? input[1] : string.Empty;

                input = sr.ReadLine().Split(splittingArray, StringSplitOptions.RemoveEmptyEntries);
                Vector3 centralPoint = new Vector3(input[1], input[2], input[3]);
                Vector3 pivotPoint = centralPoint;

                bool hasVertexColors = false;

                input = sr.ReadLine().Split(splittingArray, StringSplitOptions.RemoveEmptyEntries);
                if (input[0] == "PivotPoint=")
                {
                    pivotPoint = new Vector3(input[1], input[2], input[3]);
                }
                else if (input[0] == "VertexColors=")
                {
                    hasVertexColors = uint.Parse(input[1]) != 0;
                }

                int vertexCount = int.Parse(sr.ReadLine().Split(splittingArray, StringSplitOptions.RemoveEmptyEntries)[1]);
                List<Vector3> vertices = new List<Vector3>(vertexCount);
                List<ColorRGBA4B> vertexColors = new List<ColorRGBA4B>(vertexCount);
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices.Add(new Vector3(sr));
                }

                if (hasVertexColors)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        vertexColors.Add(new ColorRGBA4B(sr));
                    }
                }

                int faceCount = int.Parse(sr.ReadLine().Split(splittingArray, StringSplitOptions.RemoveEmptyEntries)[1]);
                List<StaticObjectFace> faces = new List<StaticObjectFace>(faceCount);
                for (int i = 0; i < faceCount; i++)
                {
                    faces.Add(new StaticObjectFace(sr));
                }

                return new StaticObject(name, CreateSubmeshes(vertices, vertexColors, faces), pivotPoint);
            }
        }

        private static List<StaticObjectSubmesh> CreateSubmeshes(List<Vector3> vertices, List<ColorRGBA4B> vertexColors, List<StaticObjectFace> faces)
        {
            bool hasVertexColors = vertexColors.Count != 0;
            Dictionary<string, List<StaticObjectFace>> submeshMap = CreateSubmeshMap(faces);
            List<StaticObjectSubmesh> submeshes = new List<StaticObjectSubmesh>();

            foreach (KeyValuePair<string, List<StaticObjectFace>> mappedSubmesh in submeshMap)
            {
                //Collect all indices and build UV map
                List<uint> indices = new List<uint>(mappedSubmesh.Value.Count * 3);
                Dictionary<uint, Vector2> uvMap = new Dictionary<uint, Vector2>(mappedSubmesh.Value.Count * 3);
                foreach (StaticObjectFace face in mappedSubmesh.Value)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        uint index = face.Indices[i];

                        indices.Add(index);

                        if (!uvMap.ContainsKey(index))
                        {
                            uvMap.Add(index, face.UVs[i]);
                        }
                    }
                }

                //Get Vertex range from indices
                uint minVertex = indices.Min();
                uint maxVertex = indices.Max();

                //Build vertex list
                uint vertexCount = maxVertex - minVertex;
                List<StaticObjectVertex> submeshVertices = new List<StaticObjectVertex>((int)vertexCount);
                for (uint i = minVertex; i < maxVertex + 1; i++)
                {
                    Vector2 uv = uvMap[i];
                    ColorRGBA4B color = hasVertexColors ? vertexColors[(int)i] : null;

                    submeshVertices.Add(new StaticObjectVertex(vertices[(int)i], uv, color));
                }

                //Normalize indices
                for (int i = 0; i < indices.Count; i++)
                {
                    indices[i] -= minVertex;
                }

                submeshes.Add(new StaticObjectSubmesh(mappedSubmesh.Key, submeshVertices, indices));
            }

            return submeshes;
        }
        private static Dictionary<string, List<StaticObjectFace>> CreateSubmeshMap(List<StaticObjectFace> faces)
        {
            Dictionary<string, List<StaticObjectFace>> submeshMap = new Dictionary<string, List<StaticObjectFace>>();

            foreach (StaticObjectFace face in faces)
            {
                if (!submeshMap.ContainsKey(face.Material))
                {
                    submeshMap.Add(face.Material, new List<StaticObjectFace>());
                }

                submeshMap[face.Material].Add(face);
            }

            return submeshMap;
        }

        public void WriteSCB(string fileLocation) => WriteSCB(File.Create(fileLocation));
        public void WriteSCB(Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                List<StaticObjectVertex> vertices = GetVertices();
                List<StaticObjectFace> faces = new List<StaticObjectFace>();
                bool hasVertexColors = false;
                StaticObjectFlags flags = 0;

                foreach (StaticObjectSubmesh submesh in this.Submeshes)
                {
                    faces.AddRange(submesh.GetFaces());
                }

                foreach (StaticObjectVertex vertex in vertices)
                {
                    if (vertex.Color != null)
                    {
                        hasVertexColors = true;
                        break;
                    }
                }

                if (hasVertexColors)
                {
                    flags |= StaticObjectFlags.VERTEX_COLORS;
                }

                bw.Write(Encoding.ASCII.GetBytes("r3d2Mesh"));
                bw.Write((ushort)3);
                bw.Write((ushort)2);
                bw.Write(Encoding.ASCII.GetBytes(this.Name.PadRight(128, '\u0000')));

                bw.Write(faces.Count);
                bw.Write((uint)flags);
                GetBoundingBox().Write(bw);
                bw.Write((uint)(flags & StaticObjectFlags.VERTEX_COLORS));

                vertices.ForEach(vertex => vertex.Position.Write(bw));

                if (hasVertexColors)
                {
                    foreach (StaticObjectVertex vertex in vertices)
                    {
                        if (vertex.Color != null)
                        {
                            vertex.Color.Write(bw);
                        }
                        else
                        {
                            new ColorRGBA4B(0, 0, 0, 255).Write(bw);
                        }
                    }
                }


                GetCentralPoint().Write(bw);
                faces.ForEach(face => face.Write(bw));
            }
        }

        public void WriteSCO(string fileLocation) => WriteSCO(File.Create(fileLocation));
        public void WriteSCO(Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
                List<StaticObjectVertex> vertices = GetVertices();
                List<StaticObjectFace> faces = new List<StaticObjectFace>();
                bool hasVertexColors = false;

                foreach (StaticObjectSubmesh submesh in this.Submeshes)
                {
                    faces.AddRange(submesh.GetFaces());
                }

                foreach (StaticObjectVertex vertex in vertices)
                {
                    if (vertex.Color != null)
                    {
                        hasVertexColors = true;
                        break;
                    }
                }

                sw.WriteLine("[ObjectBegin]");
                sw.WriteLine("Name= " + this.Name);
                sw.WriteLine("CentralPoint= " + GetCentralPoint().ToString());

                if (this.PivotPoint != null)
                {
                    sw.WriteLine("PivotPoint= " + this.PivotPoint.ToString());
                }
                if (hasVertexColors)
                {
                    sw.WriteLine("VertexColors= 1");
                }

                sw.WriteLine("Verts= " + vertices.Count);
                vertices.ForEach(vertex => vertex.Position.Write(sw, true));

                if (hasVertexColors)
                {
                    foreach (StaticObjectVertex vertex in vertices)
                    {
                        if (vertex.Color != null)
                        {
                            vertex.Color.Write(sw, true);
                        }
                        else
                        {
                            new ColorRGBA4B(0, 0, 0, 255).Write(sw, true);
                        }
                    }
                }


                sw.WriteLine("Faces= " + faces.Count);
                faces.ForEach(face => face.Write(sw));

                sw.WriteLine("[ObjectEnd]");
            }
        }

        public List<StaticObjectVertex> GetVertices()
        {
            List<StaticObjectVertex> vertices = new List<StaticObjectVertex>();

            foreach (StaticObjectSubmesh submesh in this.Submeshes)
            {
                vertices.AddRange(submesh.Vertices);
            }

            return vertices;
        }
        public List<uint> GetIndices()
        {
            List<uint> indices = new List<uint>();

            uint startIndex = 0;
            foreach (StaticObjectSubmesh submesh in this.Submeshes)
            {
                indices.AddRange(submesh.Indices.Select(x => x += startIndex));

                startIndex += submesh.Indices.Max();
            }

            return indices;
        }

        public R3DBox GetBoundingBox()
        {
            Vector3 min = Vector3.Infinity;
            Vector3 max = Vector3.NegativeInfinity;

            foreach (StaticObjectSubmesh submesh in this.Submeshes)
            {
                foreach (StaticObjectVertex vertex in submesh.Vertices)
                {
                    if (min.X > vertex.Position.X) min.X = vertex.Position.X;
                    if (min.Y > vertex.Position.Y) min.Y = vertex.Position.Y;
                    if (min.Z > vertex.Position.Z) min.Z = vertex.Position.Z;
                    if (max.X < vertex.Position.X) max.X = vertex.Position.X;
                    if (max.Y < vertex.Position.Y) max.Y = vertex.Position.Y;
                    if (max.Z < vertex.Position.Z) max.Z = vertex.Position.Z;
                }
            }

            return new R3DBox(min, max);
        }
        public Vector3 GetCentralPoint() => GetBoundingBox().GetCentralPoint();

        public void Load(string name)
        {
            List<StaticObjectVertex> vertices = GetVertices();
            List<uint> indices = GetIndices();

            MIntArray polygonIndexCounts = new MIntArray((uint)indices.Count / 3);
            MIntArray polygonIndices = new MIntArray((uint)indices.Count);
            MFloatPointArray meshVertices = new MFloatPointArray((uint)vertices.Count);
            MFloatArray arrayU = new MFloatArray((uint)vertices.Count);
            MFloatArray arrayV = new MFloatArray((uint)vertices.Count);
            MFnMesh mesh = new MFnMesh();
            MDagPath meshDagPath = new MDagPath();
            MDGModifier modifier = new MDGModifier();
            MFnSet set = new MFnSet();

            for (int i = 0; i < indices.Count / 3; i++)
            {
                polygonIndexCounts[i] = 3;
            }

            for (int i = 0; i < indices.Count; i++)
            {
                polygonIndices[i] = (int)indices[i];
            }

            for (int i = 0; i < vertices.Count; i++)
            {
                StaticObjectVertex vertex = vertices[i];

                meshVertices[i] = new MFloatPoint(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                arrayU[i] = vertex.UV.X;
                arrayV[i] = 1 - vertex.UV.Y;
            }

            //Assign mesh data
            mesh.create(vertices.Count, indices.Count / 3, meshVertices, polygonIndexCounts, polygonIndices, arrayU, arrayV, MObject.kNullObj);
            mesh.getPath(meshDagPath);
            mesh.assignUVs(polygonIndexCounts, polygonIndices);

            //Set names
            mesh.setName(name);
            MFnTransform transformNode = new MFnTransform(mesh.parent(0));
            transformNode.setName("transform_" + name);

            //Get render partition
            MFnPartition renderPartition = MayaHelper.FindRenderPartition();

            //Create Materials
            uint startIndex = 0;
            for (int i = 0; i < this.Submeshes.Count; i++)
            {
                MFnDependencyNode dependencyNode = new MFnDependencyNode();
                MFnLambertShader lambertShader = new MFnLambertShader();
                StaticObjectSubmesh submesh = this.Submeshes[i];

                lambertShader.create(true);
                lambertShader.setName(submesh.Name);
                lambertShader.color = MaterialProvider.GetMayaColor(i);

                MObject shadingEngine = dependencyNode.create("shadingEngine", submesh.Name + "_SG");
                MObject materialInfo = dependencyNode.create("materialInfo", submesh.Name + "_MaterialInfo");
                MPlug partitionPlug = new MFnDependencyNode(shadingEngine).findPlug("partition");
                MPlug setsPlug = MayaHelper.FindFirstNotConnectedElement(renderPartition.findPlug("sets"));
                modifier.connect(partitionPlug, setsPlug);

                MPlug outColorPlug = lambertShader.findPlug("outColor");
                MPlug surfaceShaderPlug = new MFnDependencyNode(shadingEngine).findPlug("surfaceShader");
                modifier.connect(outColorPlug, surfaceShaderPlug);

                MPlug messagePlug = new MFnDependencyNode(shadingEngine).findPlug("message");
                MPlug shadingGroupPlug = new MFnDependencyNode(materialInfo).findPlug("shadingGroup");
                modifier.connect(messagePlug, shadingGroupPlug);

                modifier.doIt();

                MFnSingleIndexedComponent component = new MFnSingleIndexedComponent();
                MObject faceComponent = component.create(MFn.Type.kMeshPolygonComponent);
                MIntArray groupPolygonIndices = new MIntArray();
                uint endIndex = (startIndex + (uint)submesh.Indices.Count) / 3;
                for (uint j = startIndex / 3; j < endIndex; j++)
                {
                    groupPolygonIndices.append((int)j);
                }
                component.addElements(groupPolygonIndices);

                set.setObject(shadingEngine);
                set.addMember(meshDagPath, faceComponent);

                startIndex += (uint)submesh.Indices.Count;
            }

            mesh.updateSurface();
        }

        public static StaticObject Create()
        {
            MDagPath meshDagPath = new MDagPath();
            MItSelectionList selectionIterator = MayaHelper.GetActiveSelectionListIterator(MFn.Type.kMesh);

            selectionIterator.getDagPath(meshDagPath);
            MFnMesh mesh = new MFnMesh(meshDagPath);

            selectionIterator.next();
            if (!selectionIterator.isDone)
            {
                MGlobal.displayError("StaticObject:Create - More than 1 mesh is selected");
                throw new Exception("StaticObject:Create - More than 1 mesh is selected");
            }

            if (!mesh.IsTriangulated())
            {
                MGlobal.displayError("StaticObject:Create - Mesh isn't triangulated");
                throw new Exception("StaticObject:Create - Mesh isn't triangulated");
            }
            if (mesh.ContainsHoles())
            {
                MGlobal.displayError("StaticObject:Create - Mesh Contains holes");
                throw new Exception("StaticObject:Create - Mesh Contains holes");
            }

            MayaMeshData meshData = mesh.GetMeshData();
            Dictionary<int, string> shaderNames = new Dictionary<int, string>();
            List<StaticObjectFace> faces = new List<StaticObjectFace>();

            //Build Shader Name map
            for (int i = 0; i < meshData.ShaderIndices.Count; i++)
            {
                int shaderIndex = meshData.ShaderIndices[i];

                if (!shaderNames.ContainsKey(shaderIndex))
                {
                    MPlug shaderPlug = new MFnDependencyNode(meshData.Shaders[shaderIndex]).findPlug("surfaceShader");
                    MPlugArray plugArray = new MPlugArray();

                    shaderPlug.connectedTo(plugArray, true, false);

                    MFnDependencyNode material = new MFnDependencyNode(shaderPlug[0].node);

                    shaderNames.Add(shaderIndex, material.name);
                }
            }

            //Construct faces
            int currentIndex = 0;
            for (int polygonIndex = 0; polygonIndex < mesh.numPolygons; polygonIndex++)
            {
                int shaderIndex = meshData.ShaderIndices[polygonIndex];
                uint[] faceIndices =
                {
                    (uint)meshData.TriangleVertices[currentIndex],
                    (uint)meshData.TriangleVertices[currentIndex + 1],
                    (uint)meshData.TriangleVertices[currentIndex + 2]
                };
                Vector2[] uvs =
                {
                    new Vector2(meshData.UArray[currentIndex], 1 - meshData.VArray[currentIndex]),
                    new Vector2(meshData.UArray[currentIndex + 1], 1 - meshData.VArray[currentIndex + 1]),
                    new Vector2(meshData.UArray[currentIndex + 2], 1 - meshData.VArray[currentIndex + 2])
                };


                faces.Add(new StaticObjectFace(faceIndices, shaderNames[shaderIndex], uvs));
                currentIndex += 3;
            }

            return new StaticObject(CreateSubmeshes(meshData.VertexArray.ToVector3List(), new List<ColorRGBA4B>(), faces));
        }
    }

    [Flags]
    public enum StaticObjectFlags : uint
    {
        VERTEX_COLORS = 1,
        LOCAL_ORIGIN_LOCATOR_AND_PIVOT = 2
    }
}

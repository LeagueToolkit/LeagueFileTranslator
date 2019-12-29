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

        private static readonly List<MColor> MATERIAL_COLORS = new List<MColor>()
        {
            new MColor(MColor.MColorType.kRGB, 0.5f, 0.5f, 0.5f),
            new MColor(MColor.MColorType.kRGB, 51, 230, 224),
            new MColor(MColor.MColorType.kRGB, 51, 230, 129),
            new MColor(MColor.MColorType.kRGB, 195, 230, 51),
            new MColor(MColor.MColorType.kRGB, 230, 177, 51),
            new MColor(MColor.MColorType.kRGB, 230, 63, 51),
            new MColor(MColor.MColorType.kRGB, 230, 51, 171),
            new MColor(MColor.MColorType.kRGB, 212, 51, 230),
            new MColor(MColor.MColorType.kRGB, 99, 51, 230),
            new MColor(MColor.MColorType.kRGB, 34, 88, 238),
            new MColor(MColor.MColorType.kRGB, 0, 162, 255),
        };

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

        public void Load(string name)
        {
            MIntArray polygonIndexCounts = new MIntArray((uint)this.Indices.Count / 3);
            MIntArray polygonIndices = new MIntArray((uint)this.Indices.Count);
            MFloatPointArray vertices = new MFloatPointArray((uint)this.Vertices.Count);
            MFloatArray arrayU = new MFloatArray((uint)this.Vertices.Count);
            MFloatArray arrayV = new MFloatArray((uint)this.Vertices.Count);
            MVectorArray normals = new MVectorArray((uint)this.Vertices.Count);
            MIntArray normalIndices = new MIntArray((uint)this.Vertices.Count);
            MFnMesh mesh = new MFnMesh();
            MDagPath meshDagPath = new MDagPath();
            MFnLambertShader lambertShader = new MFnLambertShader();
            MFnDependencyNode dependencyNode = new MFnDependencyNode();
            MDGModifier modifier = new MDGModifier();
            MFnSet set = new MFnSet();

            for (int i = 0; i < this.Indices.Count / 3; i++)
            {
                polygonIndexCounts[i] = 3;
            }

            for (int i = 0; i < this.Indices.Count; i++)
            {
                polygonIndices[i] = this.Indices[i];
            }

            for (int i = 0; i < this.Vertices.Count; i++)
            {
                SKNVertex vertex = this.Vertices[i];

                vertices[i] = new MFloatPoint(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                arrayU[i] = vertex.UV.X;
                arrayV[i] = 1 - vertex.UV.Y;
                normals[i] = new MVector(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                normalIndices[i] = i;
            }

            //Assign mesh data
            mesh.create(this.Vertices.Count, this.Indices.Count / 3, vertices, polygonIndexCounts, polygonIndices, arrayU, arrayV, MObject.kNullObj);
            mesh.setVertexNormals(normals, normalIndices);
            mesh.getPath(meshDagPath);
            mesh.assignUVs(polygonIndexCounts, polygonIndices);

            //Set names
            mesh.setName(name);
            MFnTransform transformNode = new MFnTransform(mesh.parent(0));
            transformNode.setName("transform_" + name);

            //Get render partition
            MGlobal.displayInfo("SKNFile:Load - Searching for Render Partition");
            MItDependencyNodes itDependencyNodes = new MItDependencyNodes(MFn.Type.kPartition);
            MFnPartition renderPartition = new MFnPartition();
            bool foundRenderPartition = false;
            MGlobal.displayInfo("SKNFile:Load - IsDone: " + itDependencyNodes.isDone);
            for (; !itDependencyNodes.isDone; itDependencyNodes.next())
            {
                renderPartition.setObject(itDependencyNodes.thisNode);
                MGlobal.displayInfo("SKNFile:Load - Iterating through partition: " + renderPartition.name + " IsRenderPartition: " + renderPartition.isRenderPartition);
                if (renderPartition.name == "renderPartition" && renderPartition.isRenderPartition)
                {
                    MGlobal.displayInfo("SKNFile:Load - Found render partition");
                    foundRenderPartition = true;
                    break;
                }
            }


            //Create Materials
            for (int i = 0; i < this.Submeshes.Count; i++)
            {
                SKNSubmesh submesh = this.Submeshes[i];
                MObject shader = lambertShader.create(true);

                lambertShader.setName(submesh.Name);
                if (i < MATERIAL_COLORS.Count)
                {
                    lambertShader.color = MATERIAL_COLORS[i];
                }

                MObject shadingEngine = dependencyNode.create("shadingEngine", submesh.Name + "_SG");
                MObject materialInfo = dependencyNode.create("materialInfo", submesh.Name + "_MaterialInfo");
                if (foundRenderPartition)
                {
                    MPlug partitionPlug = new MFnDependencyNode(shadingEngine).findPlug("partition");
                    MPlug setsPlug = FindFirstNotConnectedElement(renderPartition.findPlug("sets"));
                    modifier.connect(partitionPlug, setsPlug);
                }
                else
                {
                    MGlobal.displayInfo("SKNFile:Load - Couldn't find Render Partition for mesh: " + name + "." + submesh.Name);
                }

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
                uint endIndex = (submesh.StartIndex + submesh.IndexCount) / 3;
                for (uint j = submesh.StartIndex / 3; j < endIndex; j++)
                {
                    groupPolygonIndices.append((int)j);
                }
                component.addElements(groupPolygonIndices);

                set.setObject(shadingEngine);
                set.addMember(meshDagPath, faceComponent);
            }

            mesh.updateSurface();
        }

        private MPlug FindFirstNotConnectedElement(MPlug plug)
        {
            MPlug returnPlug = new MPlug();
            MIntArray usedIndices = new MIntArray();

            plug.getExistingArrayAttributeIndices(usedIndices);

            uint i = 0;
            do
            {
                returnPlug = plug.elementByLogicalIndex(i);
                i++;
            } while (returnPlug.isConnected);

            return returnPlug;
        }
    }
}

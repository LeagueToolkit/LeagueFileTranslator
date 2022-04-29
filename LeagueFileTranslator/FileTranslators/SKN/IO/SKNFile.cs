using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using Autodesk.Maya.OpenMayaUI;
using LeagueFileTranslator.FileTranslators.SKL.IO;
using LeagueFileTranslator.Helpers;
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
        public List<SKNSubmesh> Submeshes { get; set; } = new List<SKNSubmesh>();
        public List<ushort> Indices { get; set; } = new List<ushort>();
        public List<SKNVertex> Vertices { get; set; } = new List<SKNVertex>();
        public R3DBox BoundingBox { get; set; }
        public R3DSphere BoundingSphere { get; set; }

        public SKNFile(SKLFile skl)
        {
            Create(skl);
        }
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

                if (major != 0)
                {
                    uint submeshCount = br.ReadUInt32();
                    for (int i = 0; i < submeshCount; i++)
                    {
                        this.Submeshes.Add(new SKNSubmesh(br));
                    }

                    if (major == 4)
                    {
                        uint flags = br.ReadUInt32();
                    }
                }

                uint indexCount = br.ReadUInt32();
                uint vertexCount = br.ReadUInt32();

                SKNVertexType vertexType = SKNVertexType.Basic;
                if (major == 4)
                {
                    uint vertexSize = br.ReadUInt32();
                    vertexType = (SKNVertexType)br.ReadUInt32();
                    this.BoundingBox = new R3DBox(br);
                    this.BoundingSphere = new R3DSphere(br);
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

        public void Write(string fileLocation)
        {
            Write(File.Create(fileLocation));
        }
        public void Write(Stream stream)
        {
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                bw.Write(0x00112233);
                bw.Write((ushort)4);
                bw.Write((ushort)1);

                bw.Write(this.Submeshes.Count);
                foreach (SKNSubmesh submesh in this.Submeshes)
                {
                    submesh.Write(bw);
                }

                bw.Write(0);
                bw.Write(this.Indices.Count);
                bw.Write(this.Vertices.Count);

                bw.Write(52); //Vertex Size
                bw.Write(0); //Vertex Type

                if (this.BoundingBox == null || this.BoundingSphere == null)
                {
                    CalculateBoundaries();
                }

                this.BoundingBox.Write(bw);
                this.BoundingSphere.Write(bw);

                for (int i = 0; i < this.Indices.Count; i++)
                {
                    bw.Write(this.Indices[i]);
                }

                for (int i = 0; i < this.Vertices.Count; i++)
                {
                    this.Vertices[i].Write(bw);
                }

                bw.Write(new byte[12]);
            }
        }

        public void CalculateBoundaries()
        {
            Vector3 min = Vector3.Infinity;
            Vector3 max = Vector3.NegativeInfinity;

            foreach (SKNVertex vertex in this.Vertices)
            {
                if (min.X > vertex.Position.X) min.X = vertex.Position.X;
                if (min.Y > vertex.Position.Y) min.Y = vertex.Position.Y;
                if (min.Z > vertex.Position.Z) min.Z = vertex.Position.Z;
                if (max.X < vertex.Position.X) max.X = vertex.Position.X;
                if (max.Y < vertex.Position.Y) max.Y = vertex.Position.Y;
                if (max.Z < vertex.Position.Z) max.Z = vertex.Position.Z;
            }

            this.BoundingBox = new R3DBox(min, max);
            Vector3 centralPoint = new Vector3(
                0.5f * (this.BoundingBox.Max.X + this.BoundingBox.Min.X),
                0.5f * (this.BoundingBox.Max.Y + this.BoundingBox.Min.Y),
                0.5f * (this.BoundingBox.Max.Z + this.BoundingBox.Min.Z));
            this.BoundingSphere = new R3DSphere(centralPoint, Vector3.Distance(centralPoint, this.BoundingBox.Max));
        }

        public void Create(SKLFile skl)
        {
            MSelectionList currentSelection = MGlobal.activeSelectionList;
            MItSelectionList currentSelectionIterator = new MItSelectionList(currentSelection, MFn.Type.kMesh);
            MDagPath meshDagPath = new MDagPath();

            if (currentSelectionIterator.isDone)
            {
                MGlobal.displayError("SKNFile:Create - No mesh selected!");
                throw new Exception("SKNFile:Create - No mesh selected!");
            }
            else
            {
                currentSelectionIterator.getDagPath(meshDagPath);
                currentSelectionIterator.next();

                if (!currentSelectionIterator.isDone)
                {
                    MGlobal.displayError("SKNFile:Create - More than one mesh selected!");
                    throw new Exception("SKNFile:Create - More than one mesh selected!");
                }
            }

            MFnMesh mesh = new MFnMesh(meshDagPath);

            //Find Skin Cluster
            MPlug inMeshPlug = mesh.findPlug("inMesh");
            MPlugArray inMeshConnections = new MPlugArray();

            inMeshPlug.connectedTo(inMeshConnections, true, false);
            if (inMeshConnections.length == 0)
            {
                MGlobal.displayError("SKNFile:Create - Failed to find Skin Cluster!");
                throw new Exception("SKNFile:Create - Failed to find Skin Cluster!");
            }

            MPlug outputGeometryPlug = inMeshConnections[0];
            MFnSkinCluster skinCluster = new MFnSkinCluster(outputGeometryPlug.node);
            MDagPathArray influenceDagPaths = new MDagPathArray();
            uint influenceCount = skinCluster.influenceObjects(influenceDagPaths);

            MGlobal.displayInfo("SKNFile:Create - Influence Count: " + influenceCount);

            //Get SKL Influence Indices
            MIntArray sklInfluenceIndices = new MIntArray(influenceCount);
            for (int i = 0; i < influenceCount; i++)
            {
                MDagPath jointDagPath = influenceDagPaths[i];

                MGlobal.displayInfo(jointDagPath.fullPathName);

                //Loop through Joint DAG Paths, if we find a math for the influence, write the index
                for (int j = 0; j < skl.JointDagPaths.Count; j++)
                {
                    if (jointDagPath.equalEqual(skl.JointDagPaths[j]))
                    {
                        MGlobal.displayInfo("Found coresponding DAG path");
                        sklInfluenceIndices[i] = j;
                        break;
                    }
                }
            }

            //Add Influence indices to SKL File
            MIntArray maskInfluenceIndex = new MIntArray(influenceCount);
            for (int i = 0; i < influenceCount; i++)
            {
                maskInfluenceIndex[i] = i;
                skl.Influences.Add((short)sklInfluenceIndices[i]);
            }

            MObjectArray shaders = new MObjectArray();
            MIntArray polygonShaderIndices = new MIntArray();
            mesh.getConnectedShaders(meshDagPath.isInstanced ? meshDagPath.instanceNumber : 0, shaders, polygonShaderIndices);

            uint shaderCount = shaders.length;
            if (shaderCount > 32) //iirc 32 is the limit of how many submeshes there can be for an SKN file
            {
                MGlobal.displayError("SKNFile:Create - You've exceeded the maximum limit of 32 shaders");
                throw new Exception("SKNFile:Create - You've exceeded the maximum limit of 32 shaders");
            }

            MIntArray vertexShaders = new MIntArray();
            ValidateMeshTopology(mesh, meshDagPath, polygonShaderIndices, ref vertexShaders, shaderCount);

            //Get Weights
            MFnSingleIndexedComponent vertexIndexedComponent = new MFnSingleIndexedComponent();
            MObject vertexComponent = vertexIndexedComponent.create(MFn.Type.kMeshVertComponent);
            MIntArray groupVertexIndices = new MIntArray((uint)mesh.numVertices);

            for (int i = 0; i < mesh.numVertices; i++)
            {
                groupVertexIndices[i] = i;
            }
            vertexIndexedComponent.addElements(groupVertexIndices);

            MDoubleArray weights = new MDoubleArray();
            uint weightsInfluenceCount = 0;
            skinCluster.getWeights(meshDagPath, vertexComponent, weights, ref weightsInfluenceCount);

            //Check if vertices don't have more than 4 influences and normalize weights
            for (int i = 0; i < mesh.numVertices; i++)
            {
                int vertexInfluenceCount = 0;
                double weightSum = 0;
                for (int j = 0; j < weightsInfluenceCount; j++)
                {
                    double weight = weights[(int)(i * weightsInfluenceCount) + j];
                    if (weight != 0)
                    {
                        vertexInfluenceCount++;
                        weightSum += weight;
                    }
                }

                if (vertexInfluenceCount > 4)
                {
                    MGlobal.displayError("SKNFile:Create - Mesh contains a vertex with more than 4 influences");
                    throw new Exception("SKNFile:Create - Mesh contains a vertex with more than 4 influences");
                }

                //Normalize weights
                for (int j = 0; j < weightsInfluenceCount; j++)
                {
                    weights[(int)(i * influenceCount) + j] /= weightSum;
                }
            }

            List<MIntArray> shaderVertexIndices = new List<MIntArray>();
            List<List<SKNVertex>> shaderVertices = new List<List<SKNVertex>>();
            List<MIntArray> shaderIndices = new List<MIntArray>();

            for (int i = 0; i < shaderCount; i++)
            {
                shaderVertexIndices.Add(new MIntArray());
                shaderVertices.Add(new List<SKNVertex>());
                shaderIndices.Add(new MIntArray());
            }

            MItMeshVertex meshVertexIterator = new MItMeshVertex(meshDagPath);
            for (meshVertexIterator.reset(); !meshVertexIterator.isDone; meshVertexIterator.next())
            {
                int index = meshVertexIterator.index();
                int shader = vertexShaders[index];
                if (shader == -1)
                {
                    MGlobal.displayWarning("SKNFile:Create - Mesh contains a vertex with no shader");
                    continue;
                }

                MPoint pointPosition = meshVertexIterator.position(MSpace.Space.kWorld);
                Vector3 position = new Vector3((float)pointPosition.x, (float)pointPosition.y, (float)pointPosition.z);
                MVectorArray normals = new MVectorArray();
                MIntArray uvIndices = new MIntArray();
                Vector3 normal = new Vector3();
                byte[] weightIndices = new byte[4];
                float[] vertexWeights = new float[4];

                meshVertexIterator.getNormals(normals);

                //Normalize normals
                for (int i = 0; i < normals.length; i++)
                {
                    normal.X += (float)normals[i].x;
                    normal.Y += (float)normals[i].y;
                    normal.Z += (float)normals[i].z;
                }

                normal.X /= normals.length;
                normal.Y /= normals.length;
                normal.Z /= normals.length;

                //Get Weight Influences and Weights
                int weightsFound = 0;
                for (int j = 0; j < weightsInfluenceCount && weightsFound < 4; j++)
                {
                    double weight = weights[(int)(index * weightsInfluenceCount) + j];

                    if (weight != 0)
                    {
                        weightIndices[weightsFound] = (byte)maskInfluenceIndex[j];
                        vertexWeights[weightsFound] = (float)weight;
                        weightsFound++;
                    }
                }

                //Get unique UVs
                meshVertexIterator.getUVIndices(uvIndices);
                if (uvIndices.length != 0)
                {
                    List<int> seen = new List<int>();
                    for (int j = 0; j < uvIndices.length; j++)
                    {
                        int uvIndex = uvIndices[j];
                        if (!seen.Contains(uvIndex))
                        {
                            seen.Add(uvIndex);

                            float u = 0;
                            float v = 0;
                            mesh.getUV(uvIndex, ref u, ref v);

                            SKNVertex vertex = new SKNVertex(position, weightIndices, vertexWeights, normal, new Vector2(u, 1 - v));
                            vertex.UVIndex = uvIndex;

                            shaderVertices[shader].Add(vertex);
                            shaderVertexIndices[shader].append(index);
                        }
                    }
                }
                else
                {
                    MGlobal.displayError("SKNFile:Create - Mesh contains a vertex with no UVs");
                    throw new Exception("SKNFile:Create - Mesh contains a vertex with no UVs");
                }
            }

            //Convert from Maya indices to data indices
            int currentIndex = 0;
            MIntArray dataIndices = new MIntArray((uint)mesh.numVertices, -1);
            for (int i = 0; i < shaderCount; i++)
            {
                for (int j = 0; j < shaderVertexIndices[i].length; j++)
                {
                    int index = shaderVertexIndices[i][j];
                    if (dataIndices[index] == -1)
                    {
                        dataIndices[index] = currentIndex;
                        shaderVertices[i][j].DataIndex = currentIndex;
                    }
                    else
                    {
                        shaderVertices[i][j].DataIndex = dataIndices[index];
                    }

                    currentIndex++;
                }

                this.Vertices.AddRange(shaderVertices[i]);
            }

            MItMeshPolygon polygonIterator = new MItMeshPolygon(meshDagPath);
            for (polygonIterator.reset(); !polygonIterator.isDone; polygonIterator.next())
            {
                int polygonIndex = (int)polygonIterator.index();
                int shaderIndex = polygonShaderIndices[polygonIndex];

                MIntArray indices = new MIntArray();
                MPointArray points = new MPointArray();
                polygonIterator.getTriangles(points, indices);

                if (polygonIterator.hasUVsProperty)
                {
                    MIntArray vertices = new MIntArray();
                    MIntArray newIndices = new MIntArray(indices.length, -1);
                    polygonIterator.getVertices(vertices);

                    for (int i = 0; i < vertices.length; i++)
                    {
                        int dataIndex = dataIndices[vertices[i]];
                        int uvIndex;
                        polygonIterator.getUVIndex(i, out uvIndex);

                        if (dataIndex == -1 || dataIndex >= this.Vertices.Count)
                        {
                            MGlobal.displayError("SKNFIle:Create - Data Index outside of range");
                            throw new Exception("SKNFIle:Create - Data Index outside of range");
                        }

                        for (int j = dataIndex; j < this.Vertices.Count; j++)
                        {
                            if (this.Vertices[j].DataIndex != dataIndex)
                            {
                                MGlobal.displayError("SKNFIle:Create - Can't find corresponding face vertex in data");
                                throw new Exception("SKNFIle:Create - Can't find corresponding face vertex in data");
                            }
                            else if (this.Vertices[j].UVIndex == uvIndex)
                            {
                                for (int k = 0; k < indices.length; k++)
                                {
                                    if (indices[k] == vertices[i])
                                    {
                                        newIndices[k] = j;
                                    }
                                }

                                break;
                            }
                        }
                    }

                    for (int i = 0; i < newIndices.length; i++)
                    {
                        shaderIndices[shaderIndex].append(newIndices[i]);
                    }

                }
                else
                {
                    for (int i = 0; i < indices.length; i++)
                    {
                        shaderIndices[shaderIndex].append(dataIndices[indices[i]]);
                    }
                }
            }

            uint startIndex = 0;
            uint startVertex = 0;
            for (int i = 0; i < shaderCount; i++)
            {
                MPlug shaderPlug = new MFnDependencyNode(shaders[i]).findPlug("surfaceShader");
                MPlugArray plugArray = new MPlugArray();
                shaderPlug.connectedTo(plugArray, true, false);

                string name = new MFnDependencyNode(plugArray[0].node).name;
                uint indexCount = shaderIndices[i].length;
                uint vertexCount = shaderVertexIndices[i].length;

                //Copy indices to SKLFile
                for (int j = 0; j < indexCount; j++)
                {
                    this.Indices.Add((ushort)shaderIndices[i][j]);
                }

                this.Submeshes.Add(new SKNSubmesh(name, startVertex, vertexCount, startIndex, indexCount));

                startIndex += indexCount;
                startVertex += vertexCount;
            }

            MGlobal.displayInfo("SKNFile:Create - Created SKN File");
        }
        private void ValidateMeshTopology(MFnMesh mesh, MDagPath meshDagPath, MIntArray polygonShaderIndices, ref MIntArray vertexShaders, uint shaderCount)
        {
            //Check if the mesh contains holes
            MIntArray holeInfoArray = new MIntArray();
            MIntArray holeVertexArray = new MIntArray();

            mesh.getHoles(holeInfoArray, holeVertexArray);
            if (holeInfoArray.length != 0)
            {
                MGlobal.displayError("SKNFile:Create - Mesh contains holes");
                throw new Exception("SKNFile:Create - Mesh contains holes");
            }

            //Check for non-Triangulated polygons and shared shaders
            vertexShaders = new MIntArray((uint)mesh.numVertices, -1);
            MItMeshPolygon polygonIterator = new MItMeshPolygon(meshDagPath);
            for (polygonIterator.reset(); !polygonIterator.isDone; polygonIterator.next())
            {
                if (!polygonIterator.hasValidTriangulation)
                {
                    MGlobal.displayError("SKNFile:Create - Mesh contains a non-Triangulated polygon");
                    throw new Exception("SKNFile:Create - Mesh contains a non-Triangulated polygon");
                }

                int shaderIndex = polygonShaderIndices[(int)polygonIterator.index()];
                MIntArray vertices = new MIntArray();

                polygonIterator.getVertices(vertices);
                if (shaderIndex == -1)
                {
                    MGlobal.displayError("SKNFile:Create - Mesh contains a face with no shader");
                    throw new Exception("SKNFile:Create - Mesh contains a face with no shader");
                }

                for (int i = 0; i < vertices.length; i++)
                {
                    if (shaderCount > 1 && vertexShaders[vertices[i]] != -1 && shaderIndex != vertexShaders[vertices[i]])
                    {
                        MGlobal.displayError("SKNFile:Create - Mesh contains a vertex with multiple sahders");
                        throw new Exception("SKNFile:Create - Mesh contains a vertex with multiple sahders");
                    }
                    else
                    {
                        vertexShaders[vertices[i]] = shaderIndex;
                    }
                }
            }
        }

        public void Load(string name, SKLFile skl = null)
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
                MFnDependencyNode dependencyNode = new MFnDependencyNode();
                MFnLambertShader lambertShader = new MFnLambertShader();
                SKNSubmesh submesh = this.Submeshes[i];
                MObject shader = lambertShader.create(true);

                lambertShader.setName(submesh.Name);
                lambertShader.color = MaterialProvider.GetMayaColor(i);

                MObject shadingEngine = dependencyNode.create("shadingEngine", submesh.Name + "_SG");
                MObject materialInfo = dependencyNode.create("materialInfo", submesh.Name + "_MaterialInfo");
                if (foundRenderPartition)
                {
                    MPlug partitionPlug = new MFnDependencyNode(shadingEngine).findPlug("partition");
                    MPlug setsPlug = MayaHelper.FindFirstNotConnectedElement(renderPartition.findPlug("sets"));
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

            if (skl == null)
            {
                mesh.updateSurface();
            }
            else
            {
                MFnSkinCluster skinCluster = new MFnSkinCluster();
                MSelectionList jointPathsSelectionList = new MSelectionList();

                jointPathsSelectionList.add(meshDagPath);
                for (int i = 0; i < skl.Influences.Count; i++)
                {
                    short jointIndex = skl.Influences[i];
                    SKLJoint joint = skl.Joints[jointIndex];
                    jointPathsSelectionList.add(skl.JointDagPaths[jointIndex]);

                    MGlobal.displayInfo(string.Format("SKNFile:Load:Bind - Added joint [{0}] {1} to binding selection", joint.ID, joint.Name));
                }

                MGlobal.selectCommand(jointPathsSelectionList);
                MGlobal.executeCommand("skinCluster -mi 4 -tsb -n skinCluster_" + name);

                MPlug inMeshPlug = mesh.findPlug("inMesh");
                MPlugArray inMeshConnections = new MPlugArray();
                inMeshPlug.connectedTo(inMeshConnections, true, false);

                if (inMeshConnections.length == 0)
                {
                    MGlobal.displayError("SKNFile:Load:Bind - Failed to find the created Skin Cluster");
                    throw new Exception("SKNFile:Load:Bind - Failed to find the created Skin Cluster");
                }

                MPlug outputGeometryPlug = inMeshConnections[0];
                MDagPathArray influencesDagPaths = new MDagPathArray();

                skinCluster.setObject(outputGeometryPlug.node);
                skinCluster.influenceObjects(influencesDagPaths);

                MIntArray influenceIndices = new MIntArray((uint)skl.Influences.Count);
                for (int i = 0; i < skl.Influences.Count; i++)
                {
                    MDagPath influencePath = skl.JointDagPaths[skl.Influences[i]];

                    for (int j = 0; j < skl.Influences.Count; j++)
                    {
                        if (influencesDagPaths[j].partialPathName == influencePath.partialPathName)
                        {
                            influenceIndices[i] = j;
                            MGlobal.displayInfo("SKNReader:Load:Bind - Added Influence Joint: " + i + " -> " + j);
                            break;
                        }
                    }
                }

                MFnSingleIndexedComponent singleIndexedComponent = new MFnSingleIndexedComponent();
                MObject vertexComponent = singleIndexedComponent.create(MFn.Type.kMeshVertComponent);
                MIntArray groupVertexIndices = new MIntArray((uint)this.Vertices.Count);

                for (int i = 0; i < this.Vertices.Count; i++)
                {
                    groupVertexIndices[i] = i;
                }
                singleIndexedComponent.addElements(groupVertexIndices);

                MGlobal.executeCommand(string.Format("setAttr {0}.normalizeWeights 0", skinCluster.name));

                MDoubleArray weights = new MDoubleArray((uint)(this.Vertices.Count * skl.Influences.Count));
                for (int i = 0; i < this.Vertices.Count; i++)
                {
                    SKNVertex vertex = this.Vertices[i];

                    for (int j = 0; j < 4; j++)
                    {
                        double weight = vertex.Weights[j];
                        int influence = vertex.BoneIndices[j];

                        if (weight != 0)
                        {
                            weights[(i * skl.Influences.Count) + influence] = weight;
                        }
                    }
                }

                skinCluster.setWeights(meshDagPath, vertexComponent, influenceIndices, weights, false);
                MGlobal.executeCommand(string.Format("setAttr {0}.normalizeWeights 1", skinCluster.name));
                MGlobal.executeCommand(string.Format("skinPercent -normalize true {0} {1}", skinCluster.name, mesh.name));
                mesh.updateSurface();
            }

        }
    }
}

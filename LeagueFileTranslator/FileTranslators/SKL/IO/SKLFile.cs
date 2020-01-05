using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using LeagueFileTranslator.Utilities;

namespace LeagueFileTranslator.FileTranslators.SKL.IO
{
    public class SKLFile
    {
        public bool IsLegacy { get; private set; }

        public List<SKLJoint> Joints { get; set; } = new List<SKLJoint>();
        public List<short> Influences { get; set; } = new List<short>();
        public Dictionary<uint, short> JointIndices { get; set; } = new Dictionary<uint, short>();
        public string Name { get; set; }
        public string AssetName { get; set; }

        public MDagPathArray JointDagPaths { get; private set; } = new MDagPathArray();

        public SKLFile(bool create)
        {
            if (create)
            {
                Create();
            }
        }
        public SKLFile(string fileLocation) : this(File.OpenRead(fileLocation))
        {

        }
        public SKLFile(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                br.BaseStream.Seek(4, SeekOrigin.Begin);
                uint magic = br.ReadUInt32();
                br.BaseStream.Seek(0, SeekOrigin.Begin);

                if (magic == 0x22FD4FC3)
                {
                    this.IsLegacy = false;
                    ReadNew(br);
                }
                else
                {
                    this.IsLegacy = true;
                    ReadLegacy(br);
                }
            }
        }
        private void ReadNew(BinaryReader br)
        {
            uint fileSize = br.ReadUInt32();
            uint formatToken = br.ReadUInt32();
            uint version = br.ReadUInt32();
            if (version != 0)
            {
                MGlobal.displayError("SKLFile:ReadNew - Unsupported Version: " + version);
                throw new Exception("Unsupported SKL version: " + version);
            }

            ushort flags = br.ReadUInt16();
            ushort jointCount = br.ReadUInt16();
            uint influencesCount = br.ReadUInt32();
            int jointsOffset = br.ReadInt32();
            int jointIndicesOffset = br.ReadInt32();
            int influencesOffset = br.ReadInt32();
            int nameOffset = br.ReadInt32();
            int assetNameOffset = br.ReadInt32();
            int boneNamesOffset = br.ReadInt32();
            int reservedOffset1 = br.ReadInt32();
            int reservedOffset2 = br.ReadInt32();
            int reservedOffset3 = br.ReadInt32();
            int reservedOffset4 = br.ReadInt32();
            int reservedOffset5 = br.ReadInt32();

            MGlobal.displayInfo(string.Format("{0} {1} {2} {3} {4}", jointCount, influencesCount, jointsOffset, influencesOffset, jointIndicesOffset));

            if (jointsOffset > 0 && jointCount != 0) //wesmart
            {
                br.BaseStream.Seek(jointsOffset, SeekOrigin.Begin);

                for (int i = 0; i < jointCount; i++)
                {
                    this.Joints.Add(new SKLJoint(br, false));
                }
            }
            if (influencesOffset > 0 && influencesCount != 0)
            {
                br.BaseStream.Seek(influencesOffset, SeekOrigin.Begin);

                for (int i = 0; i < influencesCount; i++)
                {
                    this.Influences.Add(br.ReadInt16());
                }
            }
            if (jointIndicesOffset > 0 && jointCount != 0)
            {
                br.BaseStream.Seek(jointIndicesOffset, SeekOrigin.Begin);

                for (int i = 0; i < jointCount; i++)
                {
                    short index = br.ReadInt16();
                    br.ReadInt16(); //pad
                    uint hash = br.ReadUInt32();

                    MGlobal.displayInfo(string.Format("{0} {1}", hash, index));
                    this.JointIndices.Add(hash, index);
                }
            }
            if (nameOffset > 0)
            {
                br.BaseStream.Seek(nameOffset, SeekOrigin.Begin);
                this.Name = Text.ReadZeroTerminatedString(br);

                MGlobal.displayInfo("SKNFile - Name: " + this.Name);
            }
            if (assetNameOffset > 0)
            {
                br.BaseStream.Seek(assetNameOffset, SeekOrigin.Begin);
                this.AssetName = Text.ReadZeroTerminatedString(br);

                MGlobal.displayInfo("SKNFile - Asset Name: " + this.AssetName);
            }

            //SwitchHand();
        }
        private void ReadLegacy(BinaryReader br)
        {
            string magic = Encoding.ASCII.GetString(br.ReadBytes(8));
            if (magic != "r3d2sklt")
            {
                MGlobal.displayError("SKLFile:ReadLegacy - Invalid File Magic: " + magic);
                throw new Exception("SKLFile:ReadLegacy - Invalid File Magic: " + magic);
            }

            uint version = br.ReadUInt32();
            if (version != 1 && version != 2)
            {
                MGlobal.displayError("SKLFile:ReadLegacy - Unsupported File Version: " + version);
                throw new Exception("SKLFile:ReadLegacy - Unsupported File Version: " + version);
            }

            uint skeletonID = br.ReadUInt32();

            uint jointCount = br.ReadUInt32();
            for (int i = 0; i < jointCount; i++)
            {
                this.Joints.Add(new SKLJoint(br, true, (short)i));
            }

            if (version == 2)
            {
                uint influencesCount = br.ReadUInt32();
                for (int i = 0; i < influencesCount; i++)
                {
                    this.Influences.Add((short)br.ReadUInt32());
                }
            }
            else if (version == 1)
            {
                for (int i = 0; i < this.Joints.Count; i++)
                {
                    this.Influences.Add((short)i);
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
                bw.Write(0); //File Size, will Seek to start and write it at the end
                bw.Write(587026371); //FNV hash of the format token string
                bw.Write(0); //Version
                bw.Write((ushort)0); //Flags
                bw.Write((ushort)this.Joints.Count);
                bw.Write(this.Influences.Count);

                int jointsSectionSize = this.Joints.Count * 100;
                int influencesSectionSize = this.Influences.Count * 2;
                int jointIndicesSectonSize = this.JointIndices.Count * 8;
                int jointsOffset = 64;
                int influencesOffset = jointsOffset + jointsSectionSize;
                int jointIndicesOffset = influencesOffset + influencesSectionSize;
                int jointNamesOffset = jointIndicesOffset + jointIndicesSectonSize;

                bw.Write(jointsOffset); //Joints Offset
                bw.Write(jointIndicesOffset);
                bw.Write(influencesOffset);
                bw.Write(0); //Name offset
                bw.Write(0); //Asset Name offset
                bw.Write(jointNamesOffset);
                bw.Write(0xFFFFFFFF); //Write reserved offset field
                bw.Write(0xFFFFFFFF); //Write reserved offset field
                bw.Write(0xFFFFFFFF); //Write reserved offset field
                bw.Write(0xFFFFFFFF); //Write reserved offset field
                bw.Write(0xFFFFFFFF); //Write reserved offset field

                Dictionary<int, int> jointNameOffsets = new Dictionary<int, int>();
                bw.Seek(jointNamesOffset, SeekOrigin.Begin);
                for (int i = 0; i < this.Joints.Count; i++)
                {
                    jointNameOffsets.Add(i, (int)bw.BaseStream.Position);

                    bw.Write(Encoding.ASCII.GetBytes(this.Joints[i].Name));
                    bw.Write((byte)0);
                }

                bw.Seek(jointsOffset, SeekOrigin.Begin);
                for (int i = 0; i < this.Joints.Count; i++)
                {
                    this.Joints[i].Write(bw, jointNameOffsets[i]);
                }

                bw.Seek(influencesOffset, SeekOrigin.Begin);
                foreach (short influence in this.Influences)
                {
                    bw.Write(influence);
                }

                bw.Seek(jointIndicesOffset, SeekOrigin.Begin);
                foreach (KeyValuePair<uint, short> jointIndex in this.JointIndices)
                {
                    bw.Write(jointIndex.Value);
                    bw.Write((ushort)0);
                    bw.Write(jointIndex.Key);
                }

                uint fileSize = (uint)bw.BaseStream.Position - 4;
                bw.BaseStream.Seek(0, SeekOrigin.Begin);
                bw.Write(fileSize);
            }
        }

        public void Create()
        {
            MItDag jointIterator = new MItDag(MItDag.TraversalType.kDepthFirst, MFn.Type.kJoint);

            //Create Joints and collect their DAG Paths
            short jointID = 0;
            for (; !jointIterator.isDone; jointIterator.next())
            {
                MFnIkJoint ikJoint = new MFnIkJoint();
                MDagPath jointDagPath = new MDagPath();

                jointIterator.getPath(jointDagPath);
                this.JointDagPaths.Add(jointDagPath);
                ikJoint.setObject(jointDagPath);

                MTransformationMatrix local = new MTransformationMatrix(ikJoint.transformationMatrix);
                MTransformationMatrix inverseGlobal = new MTransformationMatrix(jointDagPath.exclusiveMatrixInverse);

                this.Joints.Add(new SKLJoint(jointID, ikJoint.name, local, inverseGlobal));
                this.JointIndices.Add(ELF.Hash(ikJoint.name), jointID);
                jointID++;
            }

            //Set Joint parents
            for (int i = 0; i < this.Joints.Count; i++)
            {
                MFnIkJoint ikJoint = new MFnIkJoint(this.JointDagPaths[i]);
                if (ikJoint.parentCount == 1 && ikJoint.parent(0).apiType == MFn.Type.kJoint)
                {
                    MFnIkJoint parentJoint = new MFnIkJoint(ikJoint.parent(0));
                    MDagPath parentDagPath = new MDagPath();
                    
                    parentJoint.getPath(parentDagPath);

                    //Find index of parent
                    for (int j = 0; j < this.JointDagPaths.Count; j++)
                    {
                        if (parentDagPath.equalEqual(this.JointDagPaths[j]))
                        {
                            this.Joints[i].ParentID = (short)j;
                        }
                    }
                }
                else
                {
                    this.Joints[i].ParentID = -1;
                }
            }

            MGlobal.displayInfo("SKLFile:Create - Created SKL File");
        }

        public void Load()
        {
            for (int i = 0; i < this.Joints.Count; i++)
            {
                MFnIkJoint ikJoint = new MFnIkJoint();
                SKLJoint joint = this.Joints[i];

                ikJoint.create();

                MDagPath jointDagPath = new MDagPath();
                ikJoint.getPath(jointDagPath);
                this.JointDagPaths.append(jointDagPath);

                ikJoint.set(joint.IsLegacy ? joint.Global : joint.Local);
                ikJoint.setName(joint.Name);
            }

            for (int i = 0; i < this.Joints.Count; i++)
            {
                SKLJoint joint = this.Joints[i];
                if (joint.ParentID == i)
                {
                    MGlobal.displayWarning(string.Format("SKLFile:Load - {0} has invalid Parent ID: {1}", joint.Name, joint.ParentID));
                }
                else if (joint.ParentID != -1) //Don't need to set up ROOT
                {
                    MFnIkJoint ikParentJoint = new MFnIkJoint(this.JointDagPaths[joint.ParentID]);
                    MFnIkJoint ikChildJoint = new MFnIkJoint(this.JointDagPaths[i]);
                    ikParentJoint.addChild(ikChildJoint.objectProperty);

                    if (this.IsLegacy)
                    {
                        MVector position = ikChildJoint.getTranslation(MSpace.Space.kTransform);
                        MQuaternion rotation = new MQuaternion();

                        ikChildJoint.getRotation(rotation, MSpace.Space.kWorld);

                        ikChildJoint.setTranslation(position, MSpace.Space.kWorld);
                        ikChildJoint.setRotation(rotation, MSpace.Space.kWorld);
                    }
                }
            }
        }
    }
}

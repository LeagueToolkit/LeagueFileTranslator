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

        public List<SKLJoint> Joints = new List<SKLJoint>();
        public List<short> ShaderJoints = new List<short>();
        public List<short> JointIndices = new List<short>();
        public string Name { get; set; }
        public string AssetName { get; set; }

        public MDagPathArray JointDagPaths = new MDagPathArray();

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
            uint shaderJointCount = br.ReadUInt32();
            int jointsOffset = br.ReadInt32();
            int jointIndicesOffset = br.ReadInt32();
            int shaderJointsOffset = br.ReadInt32();
            int nameOffset = br.ReadInt32();
            int assetNameOffset = br.ReadInt32();
            int boneNamesOffset = br.ReadInt32();
            int reservedOffset1 = br.ReadInt32();
            int reservedOffset2 = br.ReadInt32();
            int reservedOffset3 = br.ReadInt32();
            int reservedOffset4 = br.ReadInt32();
            int reservedOffset5 = br.ReadInt32();

            if (jointsOffset > 0 && jointCount != 0) //wesmart
            {
                br.BaseStream.Seek(jointsOffset, SeekOrigin.Begin);

                for (int i = 0; i < jointCount; i++)
                {
                    this.Joints.Add(new SKLJoint(br, false));
                }
            }

            if (shaderJointsOffset > 0 && shaderJointCount != 0)
            {
                br.BaseStream.Seek(shaderJointsOffset, SeekOrigin.Begin);

                for (int i = 0; i < shaderJointCount; i++)
                {
                    this.ShaderJoints.Add(br.ReadInt16());
                }
            }

            if (jointIndicesOffset > 0 && jointCount != 0)
            {
                br.BaseStream.Seek(jointIndicesOffset, SeekOrigin.Begin);

                for (int i = 0; i < jointCount; i++)
                {
                    this.JointIndices.Add(br.ReadInt16());
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
                uint jointIndicesCount = br.ReadUInt32();
                for (int i = 0; i < jointIndicesCount; i++)
                {
                    this.JointIndices.Add((short)br.ReadUInt32());
                }
            }
        }

        private void SwitchHand()
        {
            for (int i = 0; i < this.Joints.Count; i++)
            {
                MMatrix matrix = new MMatrix(this.Joints[i].Transform);
                MTransformationMatrix transform = new MTransformationMatrix(matrix);
                MQuaternion rotation = transform.rotation;

                transform.setRotationQuaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
                matrix = transform.asMatrixProperty;

                matrix[3, 0] = -matrix[3, 0];
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        this.Joints[i].Transform[j, k] = (float)matrix[(uint)j, (uint)k];
                    }
                }
            }
        }

        public void Load()
        {
            for (int i = 0; i < this.Joints.Count; i++)
            {
                this.Joints[i].ComposeTransform(this.Joints);
            }

            for (int i = 0; i < this.Joints.Count; i++)
            {
                MFnIkJoint ikJoint = new MFnIkJoint();
                SKLJoint joint = this.Joints[i];
                MMatrix matrix = new MMatrix(joint.Transform);
                MTransformationMatrix transformationMatrix = new MTransformationMatrix(matrix);

                ikJoint.create();

                MDagPath jointDagPath = new MDagPath();
                ikJoint.getPath(jointDagPath);
                this.JointDagPaths.append(jointDagPath);

                ikJoint.set(transformationMatrix);
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
                    MVector position = ikChildJoint.getTranslation(MSpace.Space.kTransform);
                    MQuaternion rotation = new MQuaternion();

                    ikChildJoint.getRotation(rotation, MSpace.Space.kWorld);

                    MObject childJointObject = ikChildJoint.objectProperty;
                    ikParentJoint.addChild(childJointObject);

                    if (true)
                    {
                        ikChildJoint.setTranslation(position, MSpace.Space.kWorld);
                        ikChildJoint.setRotation(rotation, MSpace.Space.kWorld);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Structures;
using LeagueFileTranslator.Utilities;

namespace LeagueFileTranslator.FileTranslators.SKL.IO
{
    public class SKLJoint
    {
        public bool IsLegacy { get; private set; }

        public ushort Flags { get; set; }
        public short ID { get; set; }
        public short ParentID { get; set; }
        public uint Hash { get; set; }
        public float Radius { get; set; } = 2.1f;
        public string Name { get; set; }
        public MTransformationMatrix ParentOffset { get; set; }
        public MTransformationMatrix RootOffset { get; set; }
        public MTransformationMatrix InverseRootOffset { get; set; }
        public float[,] Transform { get; set; } = new float[4, 4];

        public SKLJoint(BinaryReader br, bool isLegacy, short id = 0)
        {
            this.IsLegacy = isLegacy;

            if (isLegacy)
            {
                this.ID = id;
                this.Name = Encoding.ASCII.GetString(br.ReadBytes(32)).Replace("\0", "");
                this.ParentID = (short)br.ReadInt32();
                this.Radius = br.ReadSingle();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        this.Transform[j, i] = br.ReadSingle();
                    }
                }

                PrintInfo();
            }
            else
            {
                this.Flags = br.ReadUInt16();
                this.ID = br.ReadInt16();
                this.ParentID = br.ReadInt16();
                br.ReadInt16(); //padding
                this.Hash = br.ReadUInt32();
                this.Radius = br.ReadSingle();

                Vector3 parentOffsetTranslation = new Vector3(br);
                Vector3 parentOffsetScale = new Vector3(br);
                Quaternion parentOffsetRotation = new Quaternion(br);
                ComposeParentOffset(parentOffsetTranslation, parentOffsetScale, parentOffsetRotation);

                Vector3 inverseRootOffsetTranslation = new Vector3(br);
                Vector3 inverseRootOffsetScale = new Vector3(br);
                Quaternion inverseRootOffsetRotation = new Quaternion(br);
                ComposeInverseRootOffset(inverseRootOffsetTranslation, inverseRootOffsetScale, inverseRootOffsetRotation);

                int nameOffset = br.ReadInt32();
                long returnOffset = br.BaseStream.Position;

                br.BaseStream.Seek(returnOffset - 4 + nameOffset, SeekOrigin.Begin);
                this.Name = Text.ReadZeroTerminatedString(br);
                br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);

                PrintInfo();
                MGlobal.displayInfo(string.Format("ParentOffset - Position: {0}  |  Scale: {1}  |  Rotation: {2}",
                    parentOffsetTranslation.ToString(), parentOffsetScale.ToString(), parentOffsetRotation.ToString()));
                MGlobal.displayInfo(string.Format("InverseRootOffset - Position: {0}  |  Scale: {1}  |  Rotation: {2}",
                    inverseRootOffsetTranslation.ToString(), inverseRootOffsetScale.ToString(), inverseRootOffsetRotation.ToString()));
            }
        }

        public void ComposeTransform(List<SKLJoint> joints)
        {
            MMatrix matrix = this.ParentOffset.asMatrixProperty;

            if (this.ParentID != -1)
            {
                matrix = new MMatrix(joints[this.ParentID].Transform) * this.ParentOffset.asMatrixProperty;
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.Transform[i, j] = (float)matrix[(uint)i, (uint)j];
                }
            }
        }
        private void ComposeParentOffset(Vector3 translation, Vector3 scale, Quaternion rotation)
        {
            MTransformationMatrix transform = new MTransformationMatrix();
            transform.addTranslation(new MVector(translation.X, translation.Y, translation.Z), MSpace.Space.kTransform);
            transform.addRotationQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W, MSpace.Space.kTransform);
            transform = new MTransformationMatrix(transform.asMatrixInverse);

            this.ParentOffset = transform;
        }
        private void ComposeInverseRootOffset(Vector3 translation, Vector3 scale, Quaternion rotation)
        {
            MTransformationMatrix transform = new MTransformationMatrix();
            transform.addTranslation(new MVector(translation.X, translation.Y, translation.Z), MSpace.Space.kTransform);
            transform.addRotationQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W, MSpace.Space.kTransform);
            transform.addScale(new double[] { scale.X, scale.Y, scale.Z }, MSpace.Space.kTransform);

            this.InverseRootOffset = transform;
        }


        public void PrintInfo()
        {
            MGlobal.displayInfo(string.Format("SKLBone: {0} ID: {1} | Parent: {2} | Radius: {3}", this.Name, this.ID, this.ParentID, this.Radius));
        }
    }
}

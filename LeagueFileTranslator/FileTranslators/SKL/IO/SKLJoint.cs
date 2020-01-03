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
        public MTransformationMatrix Local { get; set; }
        public MTransformationMatrix Global { get; set; }
        public MTransformationMatrix InverseGlobal { get; set; }

        public SKLJoint(BinaryReader br, bool isLegacy, short id = 0)
        {
            this.IsLegacy = isLegacy;

            if (isLegacy)
            {
                this.ID = id;
                this.Name = Encoding.ASCII.GetString(br.ReadBytes(32)).Replace("\0", "");
                this.ParentID = (short)br.ReadInt32();
                this.Radius = br.ReadSingle();

                float[,] transform = new float[4, 4];
                transform[0, 3] = 0;
                transform[1, 3] = 0;
                transform[2, 3] = 0;
                transform[3, 3] = 1;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        transform[j, i] = br.ReadSingle();
                    }
                }

                this.Global = new MTransformationMatrix(new MMatrix(transform));

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

                Vector3 localTranslation = new Vector3(br);
                Vector3 localScale = new Vector3(br);
                Quaternion localRotation = new Quaternion(br);
                ComposeLocal(localTranslation, localScale, localRotation);

                Vector3 inverseGlobalTranslation = new Vector3(br);
                Vector3 inverseGlobalScale = new Vector3(br);
                Quaternion inverseGlobalRotation = new Quaternion(br);
                ComposeInverseGlobal(inverseGlobalTranslation, inverseGlobalScale, inverseGlobalRotation);

                int nameOffset = br.ReadInt32();
                long returnOffset = br.BaseStream.Position;

                br.BaseStream.Seek(returnOffset - 4 + nameOffset, SeekOrigin.Begin);
                this.Name = Text.ReadZeroTerminatedString(br);
                br.BaseStream.Seek(returnOffset, SeekOrigin.Begin);

                PrintInfo();
                MGlobal.displayInfo(string.Format("ParentOffset - Position: {0}  |  Scale: {1}  |  Rotation: {2}",
                    localTranslation.ToString(), localScale.ToString(), localRotation.ToString()));
                MGlobal.displayInfo(string.Format("InverseRootOffset - Position: {0}  |  Scale: {1}  |  Rotation: {2}",
                    inverseGlobalTranslation.ToString(), inverseGlobalScale.ToString(), inverseGlobalRotation.ToString()));
            }
        }

        public void ComposeTransform(List<SKLJoint> joints)
        {
            if (this.ParentID != -1)
            {
                this.Global = new MTransformationMatrix(joints[this.ParentID].Global.asMatrixProperty * this.Local.asMatrixProperty);
            }
            else
            {
                this.Global = this.Local;
            }
        }
        private void ComposeLocal(Vector3 translation, Vector3 scale, Quaternion rotation)
        {
            MTransformationMatrix transform = new MTransformationMatrix();
            transform.setTranslation(new MVector(translation.X, translation.Y, translation.Z), MSpace.Space.kWorld);
            transform.setRotationQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W, MSpace.Space.kWorld);
            transform.setScale(new double[] { scale.X, scale.Y, scale.Z }, MSpace.Space.kWorld);

            this.Local = transform;
        }
        private void ComposeInverseGlobal(Vector3 translation, Vector3 scale, Quaternion rotation)
        {
            MTransformationMatrix transform = new MTransformationMatrix();
            transform.setTranslation(new MVector(translation.X, translation.Y, translation.Z), MSpace.Space.kTransform);
            transform.setRotationQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W, MSpace.Space.kTransform);
            transform.setScale(new double[] { scale.X, scale.Y, scale.Z }, MSpace.Space.kTransform);

            this.InverseGlobal = transform;
        }


        public void PrintInfo()
        {
            MGlobal.displayInfo(string.Format("SKLBone: {0} ID: {1} | Parent: {2} | Radius: {3}", this.Name, this.ID, this.ParentID, this.Radius));
        }
    }
}

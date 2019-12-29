using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.FileTranslators.SKN;
using LeagueFileTranslator.FileTranslators.SKN.IO;

[assembly: MPxFileTranslatorClass(typeof(SKNImporter), "SKNImporter", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.SKN
{
    public class SKNImporter : MPxFileTranslator
    {
        public const string EXTENSION = "skn";

        public override void reader(MFileObject file, string optionsString, FileAccessMode mode)
        {
            bool imported = false;
            if (mode == FileAccessMode.kImportAccessMode)
            {
                SKNFile skn = new SKNFile(file.expandedFullName);

                MGlobal.displayInfo("SKN Vertex Count: " + skn.Vertices.Count);
                MGlobal.displayInfo("SKN Index Count: " + skn.Indices.Count);
                MGlobal.displayInfo("SKN Submesh Count: " + skn.Submeshes.Count);
            }
            else
            {
                throw new ArgumentException("Invalid File Access Mode: " + mode, "mode");
            }
        }

        public override bool haveReadMethod()
        {
            return true;
        }
        public override bool haveWriteMethod()
        {
            return false;
        }
        public override bool canBeOpened()
        {
            return false;
        }
        public override string defaultExtension()
        {
            return EXTENSION;
        }
    }
}

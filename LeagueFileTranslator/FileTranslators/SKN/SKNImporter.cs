using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.FileTranslators.SKL.IO;
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
            if (mode == FileAccessMode.kImportAccessMode)
            {
                string pathWithoutExtension = file.expandedFullName.Substring(0, file.expandedFullName.LastIndexOf('.'));

                SKNFile skn = new SKNFile(file.expandedFullName);
                SKLFile skl = new SKLFile(pathWithoutExtension + ".skl");

                MGlobal.displayInfo("SKNImporter:reader - SKN Vertex Count: " + skn.Vertices.Count);
                MGlobal.displayInfo("SKNImporter:reader - SKN Index Count: " + skn.Indices.Count);
                MGlobal.displayInfo("SKNImporter:reader - SKN Submesh Count: " + skn.Submeshes.Count);

                skl.Load();
                skn.Load(Path.GetFileNameWithoutExtension(file.expandedFullName), skl);
            }
            else
            {
                throw new ArgumentException("SKNImporter:reader - Invalid File Access Mode: " + mode, "mode");
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

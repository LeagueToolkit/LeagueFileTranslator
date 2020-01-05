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

[assembly: MPxFileTranslatorClass(typeof(SKNExporter), "SKN Exporter", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.SKN
{
    public class SKNExporter : MPxFileTranslator
    {
        public override void writer(MFileObject file, string optionsString, FileAccessMode mode)
        {
            if (mode == FileAccessMode.kExportActiveAccessMode)
            {
                string sknPath = file.expandedFullName;
                string sklPath = Path.ChangeExtension(sknPath, ".skl");

                SKLFile skl = new SKLFile(true);
                SKNFile skn = new SKNFile(skl);

                skl.Write(sklPath);
                skn.Write(sknPath);
            }
            else
            {
                MGlobal.displayError("SKNExporter - Wrong File Access Mode: " + mode);
            }
        }

        public override bool haveReadMethod()
        {
            return false;
        }

        public override bool haveWriteMethod()
        {
            return true;
        }

        public override string defaultExtension()
        {
            return "skn";
        }
    }
}

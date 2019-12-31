using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.FileTranslators.SCB;
using LeagueFileTranslator.FileTranslators.SCB.IO;

[assembly: MPxFileTranslatorClass(typeof(SCBImporter), "SCB Importer", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.SCB
{
    public class SCBImporter : MPxFileTranslator
    {
        public override void reader(MFileObject file, string optionsString, FileAccessMode mode)
        {
            if (mode == FileAccessMode.kImportAccessMode)
            {
                string pathWithoutExtension = file.expandedFullName.Substring(0, file.expandedFullName.LastIndexOf('.'));
                string name = Path.GetFileNameWithoutExtension(file.expandedFullName).Replace('.', '_');

                SCBFile scb = new SCBFile(file.expandedFullName);
                scb.Load(name);
            }
            else
            {
                throw new ArgumentException("SCBImporter:reader - Invalid File Access Mode: " + mode, "mode");
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
            return "scb";
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.FileTranslators.StaticObject.Maya;

[assembly: MPxFileTranslatorClass(typeof(SCBImporter), "Static Object (SCB) Importer", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.StaticObject.Maya
{
    public class SCBImporter : MPxFileTranslator
    {
        public override void reader(MFileObject file, string optionsString, FileAccessMode mode)
        {
            if (mode == FileAccessMode.kImportAccessMode)
            {
                string name = Path.GetFileNameWithoutExtension(file.expandedFullName).Replace('.', '_');

                StaticObject scb = StaticObject.ReadSCB(file.expandedFullName);
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

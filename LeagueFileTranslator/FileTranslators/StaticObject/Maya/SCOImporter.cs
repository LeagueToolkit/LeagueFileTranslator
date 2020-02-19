using Autodesk.Maya.OpenMaya;
using System;
using LeagueFileTranslator.FileTranslators.StaticObject.Maya;
using System.IO;

[assembly: MPxFileTranslatorClass(typeof(SCOImporter), "Static Object (SCO) Importer", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.StaticObject.Maya
{
    public class SCOImporter : MPxFileTranslator
    {
        public override void reader(MFileObject file, string optionsString, FileAccessMode mode)
        {
            if (mode == FileAccessMode.kImportAccessMode)
            {
                string name = Path.GetFileNameWithoutExtension(file.expandedFullName).Replace('.', '_');

                StaticObject scb = StaticObject.ReadSCO(file.expandedFullName);
                scb.Load(name);
            }
            else
            {
                throw new ArgumentException("SCOImporter:reader - Invalid File Access Mode: " + mode, "mode");
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
            return "sco";
        }
    }
}

using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.FileTranslators.StaticObject.Maya;

[assembly: MPxFileTranslatorClass(typeof(SCOExporter), "SCO Exporter", null, "", "")]
namespace LeagueFileTranslator.FileTranslators.StaticObject.Maya
{
    public class SCOExporter : MPxFileTranslator
    {
        public override void writer(MFileObject file, string optionsString, FileAccessMode mode)
        {
            if (mode == FileAccessMode.kExportActiveAccessMode)
            {
                StaticObject sco = StaticObject.Create();
                sco.WriteSCO(file.expandedFullName);
            }
            else
            {
                MGlobal.displayError("SCBExporter - Wrong File Access Mode: " + mode);
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
            return "sco";
        }
    }
}

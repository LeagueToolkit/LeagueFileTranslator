using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya;
using LeagueFileTranslator;

[assembly: MPxFileTranslatorClass(typeof(LeagueFileTranslator.SKNTranslator), "SKN Translator", "", "", "")]
namespace LeagueFileTranslator
{ 
    public class SKNTranslator : MPxFileTranslator
    {

    }
}

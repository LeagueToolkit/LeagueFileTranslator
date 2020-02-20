using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Extensions
{
    public static class MFloatPointExtensions
    {
        public static Vector3 ToVector3(this MFloatPoint floatPoint)
        {
            return new Vector3(floatPoint.x, floatPoint.y, floatPoint.z);
        }
    }
}

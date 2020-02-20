using Autodesk.Maya.OpenMaya;
using LeagueFileTranslator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Extensions
{
    public static class MFloatPointArrayExtensions
    {
        public static List<Vector3> ToVector3List(this MFloatPointArray array)
        {
            List<Vector3> list = new List<Vector3>(array.Count);

            for(int i = 0; i < array.Count; i++)
            {
                list.Add(array[i].ToVector3());
            }

            return list;
        }
    }
}

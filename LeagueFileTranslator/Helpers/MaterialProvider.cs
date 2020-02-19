using Autodesk.Maya.OpenMaya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Helpers
{
    public static class MaterialProvider
    {
        private static readonly List<MColor> MATERIAL_COLORS = new List<MColor>()
        {
            new MColor(MColor.MColorType.kRGB, 0.5f, 0.5f, 0.5f),
            new MColor(MColor.MColorType.kRGB, 51, 230, 224),
            new MColor(MColor.MColorType.kRGB, 51, 230, 129),
            new MColor(MColor.MColorType.kRGB, 195, 230, 51),
            new MColor(MColor.MColorType.kRGB, 230, 177, 51),
            new MColor(MColor.MColorType.kRGB, 230, 63, 51),
            new MColor(MColor.MColorType.kRGB, 230, 51, 171),
            new MColor(MColor.MColorType.kRGB, 212, 51, 230),
            new MColor(MColor.MColorType.kRGB, 99, 51, 230),
            new MColor(MColor.MColorType.kRGB, 34, 88, 238),
            new MColor(MColor.MColorType.kRGB, 0, 162, 255),
        };

        public static MColor GetMayaColor(int index)
        {
            if(index > MATERIAL_COLORS.Count)
            {
                return MATERIAL_COLORS[0];
            }
            else
            {
                return MATERIAL_COLORS[index];
            }
        }
    }
}

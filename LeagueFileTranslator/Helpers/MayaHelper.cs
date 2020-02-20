using Autodesk.Maya.OpenMaya;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueFileTranslator.Helpers
{
    public static class MayaHelper
    {
        public static MPlug FindFirstNotConnectedElement(MPlug plug)
        {
            uint i = 0;
            MPlug returnPlug;
            do
            {
                returnPlug = plug.elementByLogicalIndex(i);
                i++;
            } while (returnPlug.isConnected);

            return returnPlug;
        }
        public static MFnPartition FindRenderPartition()
        {
            MItDependencyNodes itDependencyNodes = new MItDependencyNodes(MFn.Type.kPartition);
            MFnPartition renderPartition = new MFnPartition();
            for (; !itDependencyNodes.isDone; itDependencyNodes.next())
            {
                renderPartition.setObject(itDependencyNodes.thisNode);
                MGlobal.displayInfo("MayaHelper:FindRenderPartition - Iterating through partition: " + renderPartition.name + " IsRenderPartition: " + renderPartition.isRenderPartition);
                if (renderPartition.name == "renderPartition" && renderPartition.isRenderPartition)
                {
                    MGlobal.displayInfo("MayaHelper:FindRenderPartition - Found render partition");
                    break;
                }
            }

            return renderPartition;
        }

        public static MItSelectionList GetActiveSelectionListIterator()
        {
            MSelectionList selectionList = new MSelectionList();
            MGlobal.getActiveSelectionList(selectionList);

            return new MItSelectionList(selectionList);
        }
        public static MItSelectionList GetActiveSelectionListIterator(MFn.Type type)
        {
            MSelectionList selectionList = new MSelectionList();
            MGlobal.getActiveSelectionList(selectionList);

            return new MItSelectionList(selectionList, type);
        }
    }
}

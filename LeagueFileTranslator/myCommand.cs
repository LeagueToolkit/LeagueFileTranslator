using System;
using Autodesk.Maya.OpenMaya;

// This line is mandatory to declare a new command in Maya
// You need to change the last parameter without your own
// node name and unique ID
[assembly: MPxCommandClass(typeof(LeagueFileTranslator.HelloWorld), "HelloWorldCSharp")]

namespace LeagueFileTranslator
{
    // This class is instantiated by Maya each time when a command 
    // is called by the user or a script.
    public class HelloWorld : MPxCommand, IMPxCommand
    {

        public override void doIt(MArgList argl)
        {
            MGlobal.displayInfo("Hello World from LeagueFileTranslator\n");
            // Put your command code here
            // ...

        }

    }

}

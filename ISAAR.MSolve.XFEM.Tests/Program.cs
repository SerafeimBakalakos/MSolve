using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.SamplesConsole.XFEM.Tests.PaperExamples;
using ISAAR.MSolve.XFEM.Tests.COMPDYN2019;

namespace ISAAR.MSolve.XFEM.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            DoubleCantileverBeam.Run();
            //Holes.Run();
            //Fillet.Run();
        }
    }
}
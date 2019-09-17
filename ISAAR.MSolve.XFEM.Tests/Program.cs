using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Paper1.DoubleCantileverBeam.Run();
            //Paper1.Holes.Run();

            COMPDYN2019.DoubleCantileverBeam.Run();
            //COMPDYN2019.Holes.Run();
            //COMPDYN2019.Fillet.Run();
        }
    }
}
using System;

namespace MGroup.XFEM.Heat
{
    class Program
    {
        static void Main(string[] args)
        {
            var simulation = new Simulation();
            simulation.Run();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Transfer;
using MPI;
using Xunit;

namespace ISAAR.MSolve.XFEM.Tests.Transfer
{
    public static class XModelMpiTests
    {
        public static void TestHolesModelTransfer(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                TestModelTransfer(10, CreateHolesModel10);
            }
        }

        internal static void TestModelTransfer(int numProcesses, Func<XModel> createModel)
        {
            //int master = 0;
            //var procs = new ProcessDistribution(Communicator.world, master, Enumerable.Range(0, numProcesses).ToArray());

            //XModel expectedModel = createModel();
            //expectedModel.ConnectDataStructures();

            //var model = new XModelMpi(procs, createModel, HolesBenchmark.ElementFactory);
            //model.ConnectDataStructures();
            //model.ScatterSubdomains();

            //// Check that everything is in its correct subdomain and has the correct state.
            //XSubdomain actualSubdomain = model.GetXSubdomain(procs.OwnSubdomainID);
            //XSubdomain expectedSubdomain = expectedModel.Subdomains[procs.OwnSubdomainID];
            //SubdomainComparisons.CheckSameNodes(expectedSubdomain, actualSubdomain);
            //SubdomainComparisons.CheckSameElements(expectedSubdomain, actualSubdomain);
            //SubdomainComparisons.CheckSameNodalLoads(expectedSubdomain, actualSubdomain);
            //SubdomainComparisons.CheckSameNodalDisplacements(expectedSubdomain, actualSubdomain);
        }

        private static XModel CreateHolesModel10()
        {
            string meshPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName
                + @"\Resources\holes_4442dofs.msh";

            HolesBenchmark benchmark = HolesBenchmark.CreateMultiSubdomainBenchmark(10, () =>
            {
                double growthLength = 1.0; // mm. Must be sufficiently larger than the element size.
                var builder = new HolesBenchmark.Builder(meshPath, growthLength);
                builder.HeavisideEnrichmentTolerance = 0.12;
                builder.MaxIterations = 12;
                builder.JintegralRadiusOverElementSize = 2.0;
                builder.TipEnrichmentRadius = 0.5;
                builder.BC = HolesBenchmark.BoundaryConditions.BottomConstrainXDisplacementY_TopConstrainXDisplacementY;
                HolesBenchmark singleSubdomainBenchmark = builder.BuildBenchmark();
                singleSubdomainBenchmark.InitializeModel();
                return singleSubdomainBenchmark;
            });

            return benchmark.Model;
        }
    }
}

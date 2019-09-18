using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Transfer;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Transfer;
using MPI;

namespace ISAAR.MSolve.XFEM.Tests.Transfer
{
    public static class XModelMpiTests
    {
        public static void TestModelTransfer(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                int master = 0;
                var procs = new ProcessDistribution(Communicator.world, master, new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
                Console.WriteLine($"Process {procs.OwnRank}: Started.");


                DcbBenchmarkBelytschko benchmark = CreateBenchmark();

                //TODO: These should be obtained by the benchmark instead of reinitializing them. This probably will not work, since 
                //      these instances will not be included in the model's XNodes.
                var enrichmentSerializer = new EnrichmentSerializer();
                enrichmentSerializer.AddEnrichment(new CrackBodyEnrichment2D(null));
                enrichmentSerializer.AddEnrichment(new CrackTipEnrichments2D(null, CrackGeometry.CrackTip.CrackTipPosition.Single));

                var model = new XModelMpi(procs, () =>
                {
                    benchmark.InitializeModel();
                    return benchmark.Model;

                },
                DcbBenchmarkBelytschko.ElementFactory, enrichmentSerializer);


                model.ConnectDataStructures();
                Console.WriteLine($"Process {procs.OwnRank}: Created model in master.");

                model.ScatterSubdomains();

                //TODO: Check that everything is in its correct subdomain and has the correct state.
            }
        }

        private static DcbBenchmarkBelytschko CreateBenchmark()
        {
            int numElementsY = 15;
            int numSubdomainsY = 3;
            int numSubdomainsX = 3 * numSubdomainsY;

            var builder = new DcbBenchmarkBelytschko.Builder(numElementsY, numSubdomainsX, numSubdomainsY);
            builder.HeavisideEnrichmentTolerance = 0.001;
            builder.MaxIterations = 8;
            builder.TipEnrichmentRadius = 0.0;
            builder.JintegralRadiusOverElementSize = 2.0;

            return builder.BuildBenchmark();
        }
    }
}

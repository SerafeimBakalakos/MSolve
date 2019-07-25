using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Transfer;
using MPI;

namespace ISAAR.MSolve.SamplesConsole.Parallel
{
    public class DistributeModelExample
    {
        private const int master = 0;

        public static void RunParallel(string[] args) //TODO: Write utility methods for the resuable code of this example
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int rank = comm.Rank;
                //Console.WriteLine($"(process {rank}) Hello World!"); // Run this to check if MPI works correctly.

                // Create the model in master process
                Model model = null;
                if (rank == master)
                {
                    model = Quad4PlateTest.CreateModel();
                    model.ConnectDataStructures();
                }

                // Serialize the data of each subdomain
                SubdomainDto[] serializedSubdomains = null;
                if (rank == master)
                {
                    int numSubdomains = model.SubdomainsDictionary.Count;
                    IReadOnlyList<Subdomain> originalSubdomains = model.Subdomains;
                    serializedSubdomains = new SubdomainDto[numSubdomains];
                    for (int s = 0; s < numSubdomains; ++s)
                    {
                        serializedSubdomains[s] = new SubdomainDto(originalSubdomains[s]);
                    }
                }

                // Scatter the serialized subdomain data from master process and deserialize in each process
                SubdomainDto serializedSubdomain = comm.Scatter(serializedSubdomains, master);
                Subdomain subdomain = serializedSubdomain.Deserialize();
                //Console.WriteLine($"(process { rank}) Subdomain { subdomain.ID}");

                // Order dofs
                subdomain.ConnectDataStructures();
                var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
                subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);

                // Create linear systems
                ILinearSystem ls = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
                ls.Reset();
                subdomain.Forces = Vector.CreateZero(ls.Size);

                // Create the stiffness matrices
                var provider = new ElementStructuralStiffnessProvider();
                var assembler = new SkylineAssembler();
                SkylineMatrix stiffness = assembler.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.Elements, provider);
                ls.Matrix = stiffness;

                // Print the trace of each stiffness matrix
                double trace = ls.Matrix.Trace();
                Console.WriteLine($"(process {rank}) Subdomain {subdomain.ID}: trace(stiffnessMatrix) = {trace}");
            }
        }
    }
}

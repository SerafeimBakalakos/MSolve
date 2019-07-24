using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Transfer;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers.Assemblers;
using ISAAR.MSolve.Solvers.LinearSystems;
using ISAAR.MSolve.Solvers.Ordering;
using ISAAR.MSolve.Solvers.Ordering.Reordering;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition;
using MPI;

namespace ISAAR.MSolve.SamplesConsole.Parallel
{
    public class DistributeModelExample
    {
        private const int master = 0;

        public static void RunParallel(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int rank = comm.Rank;
                Console.WriteLine($"(process {rank}) Hello World!");

                #region Does not work yet
                //// Create the model in master process
                //Model model = null;
                //Subdomain[] subdomains = null;
                //if (rank == master)
                //{
                //    model = CreateModel();
                //    subdomains = model.Subdomains.ToArray();
                //}

                //// Scatter subdomain data from master process
                //Subdomain subdomain = comm.Scatter(subdomains, master);

                //// Order dofs
                //subdomain.ConnectDataStructures();
                //var dofOrderer = new DofOrderer(new NodeMajorDofOrderingStrategy(), new NullReordering());
                //subdomain.FreeDofOrdering = dofOrderer.OrderFreeDofs(subdomain);

                //// Create linear systems
                //ILinearSystem ls = new SingleSubdomainSystem<SkylineMatrix>(subdomain);
                //ls.Reset();
                //subdomain.Forces = Vector.CreateZero(ls.Size);

                //// Create the stiffness matrices
                //var provider = new ElementStructuralStiffnessProvider();
                //var assembler = new SkylineAssembler();
                //SkylineMatrix stiffness = assembler.BuildGlobalMatrix(subdomain.FreeDofOrdering, subdomain.Elements, provider);
                //ls.Matrix = stiffness;

                //// Print the trace of each stiffness matrix
                //double trace = Trace(ls.Matrix);
                //Console.WriteLine($"(process {rank}) Subdomain {subdomain.ID}: trace(stiffnessMatrix) = {trace}");
                #endregion
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;

namespace ISAAR.MSolve.Logging.DomainDecomposition
{
    public class DomainDecompositionLoggerFetiDP : IDomainDecompositionLogger
    {
        private readonly string plotDirectoryPath;
        private readonly ICornerNodeSelection cornerNodeSelection;
        private readonly bool shuffleSubdomainColors;
        private int analysisStep;

        //TODO: make sure the path does not end in "\"
        public DomainDecompositionLoggerFetiDP(ICornerNodeSelection cornerNodeSelection, string plotDirectoryPath, 
            bool shuffleSubdomainColors = false) 
        {
            this.plotDirectoryPath = plotDirectoryPath;
            this.cornerNodeSelection = cornerNodeSelection;
            this.shuffleSubdomainColors = shuffleSubdomainColors;
            analysisStep = 0;
        }

        public void PlotSubdomains(IModel model)
        {
            var writer = new MeshPartitionWriter(shuffleSubdomainColors);
            writer.WriteSubdomainElements($"{plotDirectoryPath}\\subdomains_{analysisStep}.vtk", model);
            writer.WriteBoundaryNodes($"{plotDirectoryPath}\\boundary_nodes_{analysisStep}.vtk", model);
            writer.WriteSpecialNodes($"{plotDirectoryPath}\\corner_nodes_{analysisStep}.vtk", "corner_nodes", 
                cornerNodeSelection.GlobalCornerNodes);

            ++analysisStep;
        }
    }
}

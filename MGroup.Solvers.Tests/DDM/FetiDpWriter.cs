using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Logging.DomainDecomposition;
//using ISAAR.MTwin.Postprocessing.Paraview;
using MGroup.Solvers.DDM.FetiDP.Dofs;

namespace MGroup.Tests.DDM
{
	public class FetiDpWriter
	{
		private readonly string plotDirectoryPath;
		private readonly ICornerDofSelection cornerDofsSelection;
		//private readonly IMidsideNodesSelection midsideNodesSelection;
		private readonly bool shuffleSubdomainColors;
		private int analysisStep;

		//TODO: make sure the path does not end in "\"
		public FetiDpWriter(string plotDirectoryPath, ICornerDofSelection cornerDofsSelection,
			/*IMidsideNodesSelection midsideNodesSelection = null, */bool shuffleSubdomainColors = false)
		{
			this.plotDirectoryPath = plotDirectoryPath.Trim(new char[] { '/', '\\' });
			this.cornerDofsSelection = cornerDofsSelection;
			//this.midsideNodesSelection = midsideNodesSelection;
			this.shuffleSubdomainColors = shuffleSubdomainColors;
			analysisStep = 0;
		}

		//public void PlotGlobalMesh(VtkMesh mesh)
		//{
		//	var writer = new MeshPartitionWriter(shuffleSubdomainColors);
		//	string path = $"{plotDirectoryPath}\\global_mesh.vtk";
		//	writer.WriteGlobalMesh(path, mesh.VtkPoints, mesh.VtkCells);
		//}

		//public void PlotConstrainedNodes(IStructuralModel model)
		//{
		//	var writer = new MeshPartitionWriter(shuffleSubdomainColors);
		//	INode[] constrainedNodes = model.Nodes.Where(node => node.Constraints.Count > 0).ToArray();
		//	writer.WriteSpecialNodes($"{plotDirectoryPath}\\constrained_nodes_{analysisStep}.vtk", "constrained_nodes",
		//		constrainedNodes);
		//}

		//public void PlotSubdomains(IStructuralModel model)
		//{
		//	var writer = new MeshPartitionWriter(shuffleSubdomainColors);
		//	writer.WriteSubdomainElements($"{plotDirectoryPath}\\subdomains_{analysisStep}.vtk", model);
		//	writer.WriteBoundaryNodes($"{plotDirectoryPath}\\boundary_nodes_{analysisStep}.vtk", model);

		//	INode[] crosspoints = model.Nodes.Where(n => n.SubdomainsDictionary.Count > 2).ToArray();
		//	writer.WriteSpecialNodes($"{plotDirectoryPath}\\crosspoints_{analysisStep}.vtk", "crosspoints", crosspoints);

		//	INode[] cornerNodes = cornerDofsSelection.CornerNodeIDs.Select(n => ((Model)model).NodesDictionary[n]).ToArray();
		//	writer.WriteSpecialNodes($"{plotDirectoryPath}\\corner_nodes_{analysisStep}.vtk", "corner_nodes", cornerNodes);

		//	//if (midsideNodesSelection != null)
		//	//{
		//	//    writer.WriteSpecialNodes($"{plotDirectoryPath}\\midside_nodes_{analysisStep}.vtk", "midside_nodes",
		//	//    midsideNodesSelection.MidsideNodesGlobal);
		//	//}

		//	INode[] constrainedNodes = model.Nodes.Where(node => node.Constraints.Count > 0).ToArray();
		//	writer.WriteSpecialNodes($"{plotDirectoryPath}\\constrained_nodes_{analysisStep}.vtk", "constrained_nodes",
		//		constrainedNodes);

		//	++analysisStep;
		//}
	}
}

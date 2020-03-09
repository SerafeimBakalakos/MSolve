using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Elements;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Input;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP.CornerNodes;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP3d.Augmentation;

namespace ISAAR.MSolve.Solvers.Tests.DomainDecomposition.Dual.FetiDP3d.Example4x4x4Quads
{
    public static class ModelCreatorInput
    {
        public static Model CreateModel()
        {
            Model model = new Model();
            for (int s = 0; s < 8; ++s)
            {
                model.SubdomainsDictionary[s] = new Subdomain(s);
            }

            (int[] ElementIds, int[] subdomainIds, int[] NodeIds, int[] constraintIds, int[,] ElementNodes, double[,] NodeCoordinates,
                Dictionary<int, int[]> SubdElements, double[] elementStiffnessFactors) =
                GetModelCreationData();

            double E_disp = 3.5; //Gpa
            double ni_disp = 0.4;

            ElasticMaterial3D material1 = new ElasticMaterial3D()
            {
                YoungModulus = E_disp,
                PoissonRatio = ni_disp,
            };

            for (int i1 = 0; i1 < NodeIds.GetLength(0); i1++)
            {
                int nodeID = NodeIds[i1];
                double nodeCoordX = NodeCoordinates[i1, 0];
                double nodeCoordY = NodeCoordinates[i1, 1];
                double nodeCoordZ = NodeCoordinates[i1, 2];

                model.NodesDictionary.Add(nodeID, new Node(id: nodeID, x: nodeCoordX, y: nodeCoordY, z: nodeCoordZ));
            }

            for (int i1 = 0; i1 < ElementIds.GetLength(0); i1++)
            {
                Element e1 = new Element()
                {
                    ID = ElementIds[i1],
                    ElementType = new Hexa8NonLinear(new ElasticMaterial3D() { YoungModulus = elementStiffnessFactors[i1] * E_disp, PoissonRatio = ni_disp }, GaussLegendre3D.GetQuadratureWithOrder(3, 3, 3)) // dixws to e. exoume sfalma enw sto beambuilding oxi//edw kaleitai me ena orisma to Hexa8
                };

                for (int j = 0; j < 8; j++)
                {
                    e1.NodesDictionary.Add(ElementNodes[i1, j], model.NodesDictionary[ElementNodes[i1, j]]);
                }
                model.ElementsDictionary.Add(e1.ID, e1);
            }

            //for (int i1 = 0; i1 < subdomainIds.GetLength(0); i1++)
            //{
            //    for (int i2 = 0; i2 < SubdElements.GetLength(1); i2++)
            //    {
            //        int subdomainID = subdomainIds[i1];
            //        Element element = model.ElementsDictionary[SubdElements[i1, i2]];
            //        model.SubdomainsDictionary[subdomainID].Elements.Add(element.ID, element);
            //    }
            //}

            foreach (var subdId in SubdElements.Keys)
            {
                foreach(int elementId in SubdElements[subdId])
                {
                    Element element = model.ElementsDictionary[elementId];
                    model.SubdomainsDictionary[subdId].Elements.Add(element.ID, element);
                }
            }

            for (int i1 = 0; i1 < constraintIds.GetLength(0); i1++)
            {
                model.NodesDictionary[constraintIds[i1]].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationX });
                model.NodesDictionary[constraintIds[i1]].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationY });
                model.NodesDictionary[constraintIds[i1]].Constraints.Add(new Constraint { DOF = StructuralDof.TranslationZ });

            }

            // Load
            model.Loads.Add(new Load() { Node = model.NodesDictionary[63], DOF = StructuralDof.TranslationZ, Amount = 1.0 });

            return model;
        }

        public static UsedDefinedCornerNodes DefineCornerNodeSelectionSerial(IModel model)
            => new UsedDefinedCornerNodes(DefineCornerNodesSubdomainsAll(model));

        public static Dictionary<ISubdomain, HashSet<INode>> DefineCornerNodesSubdomainsAll(IModel model)
        {
            var cornerNodes = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                cornerNodes[subdomain] = new HashSet<INode>(new INode[] { model.GetNode(63) });
            }
            return cornerNodes;
        }

        public static HashSet<INode> DefineCornerNodesSubdomain(ISubdomain subdomain)
        {
            Debug.Assert(subdomain.ID >= 0 && subdomain.ID < 8);
            return new HashSet<INode>(new INode[] { subdomain.GetNode(63), });
        }

        public static UserDefinedMidsideNodes DefineMidsideNodeSelectionSerial(IModel model)
            => new UserDefinedMidsideNodes(DefineMidsideNodesAll(model), new IDofType[] { StructuralDof.TranslationX, StructuralDof.TranslationY, StructuralDof.TranslationZ } );

        public static Dictionary<ISubdomain, HashSet<INode>> DefineMidsideNodesAll(IModel model)
        {
            int[] midsideNodeIds = { 62, 64, 58, 70, 38, 100 };

            var midsideNodes = new Dictionary<ISubdomain, HashSet<INode>>();
            foreach (ISubdomain subdomain in model.EnumerateSubdomains())
            {
                midsideNodes[subdomain] = new HashSet<INode>();
                foreach (int midsideNodeId in midsideNodeIds)
                {
                    try
                    {
                        midsideNodes[subdomain].Add(subdomain.GetNode(midsideNodeId));
                    }
                    catch (Exception)
                    {}
                }
            }
            return midsideNodes;
        }

        private static (int[], int[], int[], int[], int[,], double[,], Dictionary<int, int[]>,double[]) GetModelCreationData()
        {
            var subdomainOutputPath = (new CnstValues()).exampleOutputPathGen;
            int[] ElementIds = SamplesConsole.SupportiveClasses.PrintUtilities.ReadIntVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\ElementIds.txt");

            int[] subdomainIds = SamplesConsole.SupportiveClasses.PrintUtilities.ReadIntVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\subdomainIds.txt");
                        
            int[] NodeIds = SamplesConsole.SupportiveClasses.PrintUtilities.ReadIntVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\NodeIds.txt");

            int[] constraintIds = SamplesConsole.SupportiveClasses.PrintUtilities.ReadIntVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\constraintIds.txt");

            var matReader = new FullMatrixReader(false);

            var elementNodes = matReader.ReadFile(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\ElementNodes.txt");

            int[,] ElementNodes = new int[elementNodes.NumRows, elementNodes.NumColumns];

            for (int i1 = 0; i1 < elementNodes.NumRows; i1++)
            {
                for (int i2 = 0; i2 < elementNodes.NumColumns; i2++)
                {
                    ElementNodes[i1, i2] = (int)elementNodes[i1, i2];
                }
            }

            double[,] NodeCoordinates = matReader.ReadFile(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\NodeCoordinates.txt").CopyToArray2D();

            int[] subdElementData = SamplesConsole.SupportiveClasses.PrintUtilities.ReadIntVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\subdElements.txt");

            Dictionary<int, int[]> SubdElements = SamplesConsole.SupportiveClasses.PrintUtilities.ConvertArrayToDictionary(subdElementData);

            double[] elementStiffnessFactors = SamplesConsole.SupportiveClasses.PrintUtilities.ReadVector(subdomainOutputPath + @"\model_overwrite\MsolveModel\" + @"\ElementStiffnessFactors.txt");

            return (ElementIds, subdomainIds, NodeIds, constraintIds, ElementNodes, NodeCoordinates, SubdElements, elementStiffnessFactors);
        }
    }
}

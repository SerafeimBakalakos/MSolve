﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.Direct;
using MGroup.XFEM.FEM.Elements;
using MGroup.XFEM.FEM.Mesh;
using MGroup.XFEM.FEM.Mesh.GMSH;

namespace MGroup.XFEM.Tests.FEM
{
    public static class FemUtilities
    {
        public static void ApplyBCsCantileverTension(Model model, int dim)
        {
            // Boundary conditions
            double meshTol = 1E-7;

            // Left side: Ux=Uy=Uz=0
            double minX = model.Nodes.Select(n => n.X).Min();
            foreach (var node in model.Nodes.Where(n => Math.Abs(n.X - minX) <= meshTol))
            {
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0 });
                if (dim >= 2) node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0 });
                if (dim == 3) node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = 0 });
            }

            // Right side: Fx = 100
            double maxX = model.Nodes.Select(n => n.X).Max();
            Node[] rightSideNodes = model.Nodes.Where(n => Math.Abs(n.X - maxX) <= meshTol).ToArray();
            double load = 1.0 / rightSideNodes.Length;
            foreach (var node in rightSideNodes)
            {
                model.Loads.Add(new Load() { Node = node, DOF = StructuralDof.TranslationX, Amount = load });
            }
        }

        public static Model Create3DModelFromGmsh(string gmshMeshFile)
        {
            var gmshReader = new GmshReader(gmshMeshFile, 3);
            PreprocessingMesh mesh = gmshReader.CreateMesh();

            var model = new Model();
            int subdomainID = 0;
            model.SubdomainsDictionary[subdomainID] = new Subdomain(subdomainID);

            foreach (Vertex vertex in mesh.Vertices)
            {
                model.NodesDictionary[vertex.ID] = new Node(vertex.ID, vertex.Coords[0], vertex.Coords[1], vertex.Coords[2]);
            }

            var material = new ElasticMaterial3D() { YoungModulus = 2.1E7, PoissonRatio = 0.3 };
            var dynamicProperties = new DynamicMaterial(1, 1, 1);
            var elementFactory = new ContinuumElement3DFactory();
            foreach (Cell cell in mesh.Cells)
            {
                Node[] nodes = cell.VertexIDs.Select(v => model.NodesDictionary[v]).ToArray();
                ContinuumElement3D elementType = elementFactory.CreateElement(cell.CellType, nodes, material, dynamicProperties);
                var elementWrapper = new Element() { ID = cell.ID, ElementType = elementType };
                elementWrapper.AddNodes(nodes);
                model.ElementsDictionary.Add(elementWrapper.ID, elementWrapper);
                model.SubdomainsDictionary[subdomainID].Elements.Add(elementWrapper);
            }

            return model;
        }

        public static IVectorView RunStaticLinearAnalysis(Model model, ILogFactory logFactory = null, 
            ISolverBuilder solverBuilder = null)
        {
            Console.WriteLine("Starting analysis");
            if (solverBuilder == null) solverBuilder = new SkylineSolver.Builder();
            ISolver solver = solverBuilder.BuildSolver(model);
            var problem = new ProblemStructural(model, solver);
            var linearAnalyzer = new LinearAnalyzer(model, solver, problem);
            var staticAnalyzer = new StaticAnalyzer(model, solver, problem, linearAnalyzer);

            // Output
            if (logFactory != null) linearAnalyzer.LogFactories[0] = logFactory;
            

            staticAnalyzer.Initialize();
            staticAnalyzer.Solve();

            Console.WriteLine("Analysis finished");
            return solver.LinearSystems[0].Solution;
        }
    }
}

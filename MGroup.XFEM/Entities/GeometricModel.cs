﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: Perhaps I should save the conforming mesh here as well, rather than in each element
namespace MGroup.XFEM.Entities
{
    public class GeometricModel
    {
        private readonly XModel physicalModel;

        public GeometricModel(int dimension, XModel physicalModel)
        {
            this.physicalModel = physicalModel;

            if (dimension != 2 && dimension != 3)
            {
                throw new ArgumentException();
            }
            this.Dimension = dimension;
        }

        public int Dimension { get; set; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public Dictionary<int, double> CalcBulkSizeOfEachPhase()
        {
            var bulkSizes = new Dictionary<int, double>();
            foreach (IPhase phase in Phases) bulkSizes[phase.ID] = 0.0;

            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if ((element.ConformingSubcells == null) || (element.ConformingSubcells.Length == 0))
                {
                    Debug.Assert(element.Phases.Count == 1);
                    IPhase phase = element.Phases.First();
                    double elementBulkSize = element.CalcBulkSize();
                    bulkSizes[phase.ID] += elementBulkSize;
                }
                else
                {
                    foreach (IElementSubcell subcell in element.ConformingSubcells)
                    {
                        var centroid = new XPoint();
                        centroid.Coordinates[CoordinateSystem.ElementNatural] = subcell.FindCentroidNatural();
                        centroid.Element = element;
                        centroid.ShapeFunctions =
                            element.Interpolation.EvaluateFunctionsAt(centroid.Coordinates[CoordinateSystem.ElementNatural]);
                        element.FindPhaseAt(centroid);
                        IPhase phase = centroid.Phase;

                        (_, double subcellBulk) = subcell.FindCentroidAndBulkSizeCartesian(element);

                        bulkSizes[phase.ID] += subcellBulk;
                    }
                }
            }

            return bulkSizes;
        }

        //TODO: Perhaps I need a dedicated class for this
        public void FindConformingMesh()
        {
            IConformingTriangulator triangulator;
            if (Dimension == 2) triangulator = new ConformingTriangulator2D();
            else if (Dimension == 3) triangulator = new ConformingTriangulator3D();
            else throw new NotImplementedException();

            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var boundaries = element.PhaseIntersections.Values.Cast<IElementGeometryIntersection>();
                if (boundaries.Count() == 0) continue;
                element.ConformingSubcells = triangulator.FindConformingMesh(element, boundaries, MeshTolerance);
            }
        }

        public void InteractWithNodes()
        {
            // Nodes
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);
        }

        public void InteractWithElements()
        {
            // Elements
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements);

        }

        public void UnifyOverlappingPhases(bool ignoreDefaultPhase)
        {
            var unifiedPhases = new List<IPhase>();
            IPhase defaultPhase = null;
            foreach (IPhase phase in Phases)
            {
                if (phase is DefaultPhase && ignoreDefaultPhase)
                {
                    defaultPhase = phase;
                    continue;
                }
                unifiedPhases.Add(phase);
            }

            int i = 0;
            while (i < unifiedPhases.Count - 1)
            {
                bool unionFound = false;
                for (int j = i + 1; j < unifiedPhases.Count; ++j)
                {
                    bool areJoined = unifiedPhases[i].UnionWith(unifiedPhases[j]);
                    if (areJoined)
                    {
                        unifiedPhases.RemoveAt(j);
                        unionFound = true;
                        break;
                    }
                }
                if (!unionFound)
                {
                    ++i;
                }
            }

            Phases.Clear();
            if (defaultPhase != null) Phases.Add(defaultPhase);
            Phases.AddRange(unifiedPhases);
        }
    }
}

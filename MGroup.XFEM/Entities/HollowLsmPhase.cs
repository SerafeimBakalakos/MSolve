using System;
using System.Collections.Generic;
using System.Diagnostics;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class HollowLsmPhase : LsmPhase
    {
        private readonly PhaseGeometryModel_OLD geometricModel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="geometricModel"></param>
        /// <param name="mergeLevel">Negative values will cause this phase to be unmergable</param>
        public HollowLsmPhase(int id, PhaseGeometryModel_OLD geometricModel, int mergeLevel) : base(id, geometricModel, mergeLevel)
        {
        }

        public List<PhaseBoundary> InternalBoundaries { get; } = new List<PhaseBoundary>();

        public HashSet<IPhase> InternalPhases { get; } = new HashSet<IPhase>();

        public override bool Contains(XNode node)
        {
            Debug.Assert(ExternalBoundaries.Count == 1);
            PhaseBoundary boundary = ExternalBoundaries[0];
            double distance = boundary.Geometry.SignedDistanceOf(node);
            bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
            sameSide |= (distance < 0) && (boundary.NegativePhase == this);
            if (!sameSide) return false;

            foreach (IPhase internalPhase in InternalPhases)
            {
                if (internalPhase.Contains(node)) return false;
            }

            return true;
        }

        public override bool Contains(XPoint point)
        {
            Debug.Assert(ExternalBoundaries.Count == 1);
            PhaseBoundary boundary = ExternalBoundaries[0];
            double distance = boundary.Geometry.SignedDistanceOf(point);
            bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
            sameSide |= (distance < 0) && (boundary.NegativePhase == this);
            if (!sameSide) return false;

            foreach (IPhase internalPhase in InternalPhases)
            {
                if (internalPhase.Contains(point)) return false;
            }

            return true;
        }

        public override bool UnionWith(IPhase otherPhase)
        {
            if (this.MergeLevel < 0) return false;
            if (this.MergeLevel != otherPhase.MergeLevel) return false;

            if (otherPhase is HollowLsmPhase otherHollowPhase)
            {
                if (this.Overlaps(otherPhase))
                {
                    // TODO: These should be enforced by this class.
                    if ((this.ExternalBoundaries.Count != 1) && (otherPhase.ExternalBoundaries.Count != 1))
                    {
                        throw new InvalidOperationException();
                    }
                    if (this.ExternalBoundaries[0].NegativePhase != this) throw new NotImplementedException();
                    if (otherPhase.ExternalBoundaries[0].NegativePhase != otherPhase) throw new NotImplementedException();
                    IPhase externalPhase = this.ExternalBoundaries[0].PositivePhase;
                    if (externalPhase != otherPhase.ExternalBoundaries[0].PositivePhase)
                    {
                        throw new NotImplementedException();
                    }

                    // Merge level sets
                    this.ExternalBoundaries[0].Geometry.UnionWith(otherPhase.ExternalBoundaries[0].Geometry);

                    // Merge external boundaries
                    externalPhase.ExternalBoundaries.Remove(otherPhase.ExternalBoundaries[0]);
                    externalPhase.Neighbors.Remove(otherPhase);
                    if (!externalPhase.ExternalBoundaries.Contains(this.ExternalBoundaries[0]))
                    {
                        externalPhase.ExternalBoundaries.Add(this.ExternalBoundaries[0]);
                    }
                    externalPhase.Neighbors.Add(this);
                    this.Neighbors.Add(externalPhase);

                    // Merge internal phases
                    this.InternalPhases.UnionWith(otherHollowPhase.InternalPhases);
                    foreach (PhaseBoundary boundary in otherHollowPhase.InternalBoundaries)
                    {
                        if (boundary.NegativePhase == otherHollowPhase) boundary.NegativePhase = this;
                        else boundary.PositivePhase = this;
                        this.InternalBoundaries.Add(boundary);
                    }

                    // Merge nodes
                    foreach (XNode node in otherPhase.ContainedNodes)
                    {
                        this.ContainedNodes.Add(node);
                        node.Phase = this;
                    }

                    // Merge elements
                    if ((this.BoundaryElements.Count != 0) && (otherHollowPhase.BoundaryElements.Count != 0))
                    {
                        throw new NotImplementedException();
                    }

                    return true;
                }
            }
            return false;
        }
    }
}

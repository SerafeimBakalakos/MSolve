using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

//TODO: Remove duplication between this and ConvexPhase
namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class HollowConvexPhase : ConvexPhase
    {
        private List<IPhase> internalPhases = new List<IPhase>();
        private HashSet<PhaseBoundary> internalPhaseBoundaries = new HashSet<PhaseBoundary>();

        public HollowConvexPhase(int id)
            : base(id)
        {
        }

        /// <summary>
        /// WARNING: The boundaries of the internal phase must first be defined.
        /// </summary>
        /// <param name="phase"></param>
        public void AddInternalPhase(IPhase phase)
        {
            internalPhases.Add(phase);
            foreach (PhaseBoundary boundary in phase.Boundaries)
            {
                //TODO: check that it is between these 2 phases
                internalPhaseBoundaries.Add(boundary);
            }
        }

        public override bool Contains(CartesianPoint point)
        {
            foreach (PhaseBoundary boundary in Boundaries.Except(internalPhaseBoundaries))
            {
                double distance = boundary.Segment.SignedDistanceOf(point);
                bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
                sameSide |= (distance < 0) && (boundary.NegativePhase == this);
                if (!sameSide) return false;
            }

            foreach (IPhase internalPhase in internalPhases)
            {
                if (internalPhase.Contains(point)) return false;
            }
            return true;
        }

        public bool Contains(CartesianPoint[] polygonVertices)
        {
            IEnumerable<PhaseBoundary> externalBoundaries = Boundaries.Except(internalPhaseBoundaries);
            foreach (CartesianPoint point in polygonVertices)
            {
                foreach (PhaseBoundary boundary in externalBoundaries)
                {
                    double distance = boundary.Segment.SignedDistanceOf(point);
                    bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
                    sameSide |= (distance < 0) && (boundary.NegativePhase == this);
                    if (!sameSide) return false;
                }
            }
            return true;
        }
    }
}

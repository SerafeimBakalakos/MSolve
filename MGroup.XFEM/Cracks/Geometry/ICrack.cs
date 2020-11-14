using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Geometry;

//TODO: This will probably be deleted, unless I use it as an abstraction of crack entity classes (2D,3D, implicit, explicit, etc), 
//      instead of crack geometry classes.
//TODO: Extend to the case where there are 2 tips! Perhaps abstract the number of tips by using a general ICrackTip that can
//      have an implementation with 2 tips. 
//TODO: Perhaps use IDs instead of references to IXCrackElement.
namespace MGroup.XFEM.Cracks.Geometry
{
    public interface ICrack
    {
        /// <summary>
        /// Elements whose edges conform to the crack, instead of being intersected by it. This does not include elements 
        /// belonging to <see cref="TipElements"/>.
        /// </summary>
        HashSet<IXCrackElement> ConformingElements { get; }

        CrackStepEnrichment CrackBodyEnrichment { get; }

        IXGeometryDescription CrackGeometry { get; }

        IReadOnlyList<ICrackTipEnrichment> CrackTipEnrichments { get; }

        /// <summary>
        /// Elements that are intersected by the crack, but do not belong to <see cref="TipElements"/>.
        /// </summary>
        HashSet<IXCrackElement> IntersectedElements { get; }

        int ID { get; }

        double[] TipCoordinates { get; }

        /// <summary>
        /// Elements containing the crack tip, not elements whose nodes are enriched with crack tip functions. In 2D cracks there 
        /// is only 1 usually. However it is possible for the tip to lie on the boundary between multiple elements.
        /// </summary>
        HashSet<IXCrackElement> TipElements { get; }

        TipCoordinateSystem TipSystem { get; } //TODO: This should probably be provided by the Geometry property

        void InteractWithMesh(); //TODO: Should this be included in Propagate()?

        void Propagate(Dictionary<int, Vector> subdomainFreeDisplacements); //TODO: What about Initialize()? Should the user call it?
    }
}

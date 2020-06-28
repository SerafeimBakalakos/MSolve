using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public interface IElementGeometry
    {
        /// <summary>
        /// 1D: calculates length. 2D: calculates area. 3D: calculates volume
        /// </summary>
        /// <param name="nodes"></param>
        double CalcBulkSize(IReadOnlyList<XNode> nodes);

        (ElementEdge[], ElementFace[]) FindEdgesFaces(IReadOnlyList<XNode> nodes);
    }

    public static class Extensions //TODO: better use default implementation
    {
        //TODO: this should be a default interface implementation.
        public static double[] FindCentroidCartesian(this IElementGeometry elemGeom, int dimension, IReadOnlyList<XNode> nodes) 
        {
            var centroid = new double[dimension];
            for (int n = 0; n < nodes.Count; ++n)
            {
                for (int j = 0; j < dimension; ++j)
                {
                    centroid[j] += nodes[n].Coordinates[j];
                }
            }
            for (int j = 0; j < dimension; ++j)
            {
                centroid[j] /= nodes.Count;
            }
            return centroid;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Plotting.Fields
{
    public class TemperatureAtNodesField
    {
        private readonly XModel model;

        public TemperatureAtNodesField(XModel model)
        {
            this.model = model;
        }

        public Dictionary<double[], double> CalcValuesAtVertices(IVectorView solution)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            XSubdomain subdomain = model.Subdomains.First().Value;
            DofTable dofTable = subdomain.FreeDofOrdering.FreeDofs;

            var result = new Dictionary<double[], double>();
            foreach (XNode node in model.Nodes)
            {
                bool isFreeDof = dofTable.TryGetValue(node, ThermalDof.Temperature, out int stdDof);
                if (isFreeDof) result[node.Coordinates] = solution[stdDof];
                else result[node.Coordinates] = node.Constraints[0].Amount;
            }
            return result;
        }
    }
}
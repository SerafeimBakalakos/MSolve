﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Plotting.Writers
{
    public class NodalPhasesPlotter : IPhaseObserver
    {
        private readonly double colorForDefaultPhase;
        private readonly int defaultPhaseID;
        private readonly XModel<IXMultiphaseElement> model;
        private readonly string outputDirectory;

        private int iteration;

        public NodalPhasesPlotter(string outputDirectory, XModel<IXMultiphaseElement> model, int defaultPhaseID,
            double colorForDefaultPhase)
        {
            this.outputDirectory = outputDirectory;
            this.model = model;
            this.defaultPhaseID = defaultPhaseID;
            this.colorForDefaultPhase = colorForDefaultPhase;

            iteration = 0;
        }

        public NodalPhasesPlotter(string outputDirectory, XModel<IXMultiphaseElement> model)
            : this(outputDirectory, model, int.MaxValue, int.MaxValue)
        {
        }

        public void LogGeometry()
        {
        }

        public void LogMeshInteractions()
        {
            string path = Path.Combine(outputDirectory, $"nodal_phases_t{iteration}.vtk");
            using (var writer = new VtkPointWriter(path))
            {
                var nodalPhases = new Dictionary<INode, double>();

                foreach (XNode node in model.XNodes)
                {
                    double phaseID = node.Phase.ID;
                    if (node.Phase.ID == defaultPhaseID) phaseID = colorForDefaultPhase;
                    nodalPhases[node] = phaseID;
                }

                writer.WriteScalarField("nodal_phases", nodalPhases);
            }

            ++iteration;
        }
    }
}
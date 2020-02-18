using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Input
{
    public class PhaseReader
    {
        private readonly bool defaultPhase;
        private readonly int defaultPhaseID;

        public PhaseReader(bool defaultPhase, int defaultPhaseID)
        {
            this.defaultPhase = defaultPhase;
            this.defaultPhaseID = defaultPhase ? defaultPhaseID : int.MinValue;
        }

        //TODO: These should not be hardcoded
        public double MaxX { get; } = 100.0;
        public double MaxY { get; } = 100.0;
        public double MinX { get; } = 0.0;
        public double MinY { get; } = 0.0;

        public GeometricModel ReadPhasesFromFile(string path)
        {
            var csvReader = new CsvReader();
            double[,] data = csvReader.ImportDataFromCSV(path);

            int numBoundaries = data.GetLength(0);
            int numColumns = 6;
            Debug.Assert(data.GetLength(1) == numColumns);

            // Define phases
            int minPhaseID = int.MaxValue;
            int maxPhaseID = int.MinValue;
            for (int i = 0; i < numBoundaries; ++i)
            {
                int phaseID = (int)data[i, 4];
                if (phaseID < minPhaseID) minPhaseID = phaseID;
                if (phaseID > maxPhaseID) maxPhaseID = phaseID;
            }
            var model = new GeometricModel();
            if (defaultPhase) model.Phases.Add(new DefaultPhase());
            for (int id = minPhaseID; id <= maxPhaseID; ++id) model.Phases.Add(new ConvexPhase(id));

            // Phase boundaries, assuming that for a given phase, its boundaries are given in counter-clockwise order
            for (int i = 0; i < numBoundaries; ++i)
            {
                var start = new CartesianPoint(data[i, 0], data[i, 1]);
                var end = new CartesianPoint(data[i, 2], data[i, 3]);
                int positivePhase = (int)data[i, 4];
                int negativePhase = (int)data[i, 5];
                new PhaseBoundary(new LineSegment2D(start, end), model.Phases[positivePhase], model.Phases[negativePhase]);
            }

            return model;
        }
    }
}

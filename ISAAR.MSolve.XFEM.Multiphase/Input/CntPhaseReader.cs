using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Input
{
    public class CntPhaseReader
    {
        private readonly bool defaultPhase;
        private readonly int defaultPhaseID;

        public CntPhaseReader(bool defaultPhase, int defaultPhaseID)
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

        public GeometricModel ReadPhasesFromFile(string layerPhasesPath, string inclusionPhasesPath)
        {
            // Read layer phases
            var csvReader = new CsvReader();
            double[,] data = csvReader.ImportDataFromCSV(layerPhasesPath);

            int numBoundaries = data.GetLength(0);
            int numColumns = 6;
            Debug.Assert(data.GetLength(1) == numColumns);

            // Define layer phases
            int minPhaseID = int.MaxValue;
            int maxPhaseID = int.MinValue;
            for (int i = 0; i < numBoundaries; ++i)
            {
                int phaseID = (int)data[i, 4];
                if (phaseID < minPhaseID) minPhaseID = phaseID;
                if (phaseID > maxPhaseID) maxPhaseID = phaseID;
            }
            var layerPhases = new List<HollowConvexPhase>();
            var model = new GeometricModel();
            if (defaultPhase) model.Phases.Add(new DefaultPhase());
            for (int id = minPhaseID; id <= maxPhaseID; ++id)
            {
                var phase = new HollowConvexPhase(id);
                layerPhases.Add(phase);
                model.Phases.Add(phase);
            }

            // Layer phase boundaries, assuming that for a given phase, its boundaries are given in counter-clockwise order
            for (int i = 0; i < numBoundaries; ++i)
            {
                var start = new CartesianPoint(data[i, 0], data[i, 1]);
                var end = new CartesianPoint(data[i, 2], data[i, 3]);
                int positivePhase = (int)data[i, 4];
                int negativePhase = (int)data[i, 5];
                new PhaseBoundary(new LineSegment2D(start, end), model.Phases[positivePhase], model.Phases[negativePhase]);
            }


            // Read inclusion phases
            double[,] inclusionsData = csvReader.ImportDataFromCSV(inclusionPhasesPath);
            int numInclusions = inclusionsData.GetLength(0);
            int numInclusionsColumns = 9;
            int numPointsPerInclusion = 4;
            Debug.Assert(inclusionsData.GetLength(1) == numInclusionsColumns);

            for (int i = 0; i < numInclusions; ++i)
            {
                // Define geometry of inclusion phase
                int phaseID = (int)inclusionsData[i, 8];
                #region debug
                //if (phaseID == 106)
                //{
                //    Console.WriteLine();
                //}
                #endregion
                var inclusionPhase = new ConvexPhase(phaseID);
                model.Phases.Add(inclusionPhase);
                var inclusionVertices = new CartesianPoint[numPointsPerInclusion];
                for (int j = 0; j < numPointsPerInclusion; ++j)
                {
                    double x = (double)inclusionsData[i, 2 * j];
                    double y = (double)inclusionsData[i, 2 * j + 1];
                    inclusionVertices[j] = new CartesianPoint(x, y);
                }

                // Find in which layer phase this inclusion is located.
                foreach (HollowConvexPhase layerPhase in layerPhases)
                {
                    #region debug
                    //if (layerPhase.ID == 6)
                    //{
                    //    Console.WriteLine();
                    //}
                    #endregion
                    if (layerPhase.Contains(inclusionVertices))
                    {
                        //WARNING: This assumes that the vertices of the inclusion are in clockwise order.
                        for (int k = 0; k < inclusionVertices.Length; ++k)
                        {
                            CartesianPoint start = inclusionVertices[k];
                            CartesianPoint end = inclusionVertices[(k + 1) % inclusionVertices.Length];
                            var boundary = new PhaseBoundary(new LineSegment2D(start, end), layerPhase, inclusionPhase);
                        }
                        layerPhase.AddInternalPhase(inclusionPhase);
                    }
                }

            }

            return model;
        }
    }
}

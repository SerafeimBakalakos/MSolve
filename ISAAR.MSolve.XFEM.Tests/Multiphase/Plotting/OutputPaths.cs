using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Tests.Multiphase.Plotting
{
    internal class OutputPaths
    {
        public string finiteElementMesh;
        public string conformingMesh;
        public string phasesGeometry;
        public string nodalPhases;
        public string elementPhases;
        public string stepEnrichedNodes;
        public string junctionEnrichedNodes;
        public string volumeIntegrationPoints;
        public string volumeIntegrationMesh;
        public string boundaryIntegrationPoints;
        public string boundaryIntegrationCells;
        public string boundaryIntegrationVertices;
        public string volumeIntegrationMaterials;
        public string boundaryIntegrationMaterials;
        public string boundaryIntegrationPhaseJumps;
        public string temperatureAtNodes;
        public string temperatureAtGaussPoints;
        public string temperatureField;
        public string heatFluxAtNodes;
        public string heatFluxAtGaussPoints;
        public string heatFluxField;

        public void FillAllForDirectory(string directory)
        {
            directory = directory.Trim('/') + "//";
            this.finiteElementMesh = directory + "fe_mesh.vtk";
            this.conformingMesh = directory + "conforming_mesh.vtk";
            this.phasesGeometry = directory + "phases_geometry.vtk";
            this.nodalPhases = directory + "nodal_phases.vtk";
            this.elementPhases = directory + "element_phases.vtk";
            this.stepEnrichedNodes = directory + "step_enriched_nodes.vtk";
            this.junctionEnrichedNodes = directory + "junction_enriched_nodes.vtk";
            this.volumeIntegrationPoints = directory + "volume_integration_points.vtk";
            this.volumeIntegrationMesh = directory + "volume_integration_mesh.vtk";
            this.boundaryIntegrationPoints = directory + "boundary_integration_points.vtk";
            this.boundaryIntegrationCells = directory + "boundary_integration_cells.vtk";
            this.boundaryIntegrationVertices = directory + "boundary_integration_vertices.vtk";
            this.volumeIntegrationMaterials = directory + "volume_integration_materials.vtk";
            this.boundaryIntegrationMaterials = directory + "boundary_integration_materials.vtk";
            this.boundaryIntegrationPhaseJumps = directory + "boundary_integration_phase_jumps.vtk";
            this.temperatureAtNodes = directory + "temperature_at_nodes.vtk";
            this.temperatureAtGaussPoints = directory + "temperature_at_gauss_points.vtk";
            this.temperatureField = directory + "temperature_field.vtk";
            this.heatFluxAtNodes = directory + "heat_flux_at_nodes.vtk";
            this.heatFluxAtGaussPoints = directory + "heat_flux_at_gauss_points.vtk";
            this.heatFluxField = directory + "heat_flux_field.vtk";
        }
    }
}

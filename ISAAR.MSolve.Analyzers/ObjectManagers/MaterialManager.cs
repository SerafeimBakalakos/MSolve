using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Materials.Interfaces;
using System.Linq;

namespace ISAAR.MSolve.Analyzers.ObjectManagers
{
    public class MaterialManager: IMaterialManager
    {
        private IContinuumMaterial3DDefGrad chosenMaterial;

        private List<IContinuumMaterial3DDefGrad> ghostMaterials;

        private Dictionary<IContinuumMaterial3DDefGrad, double[]> ghostMaterialStrains;
        private Dictionary<IContinuumMaterial3DDefGrad, double[]> ghostMaterialStresses;
        private Dictionary<IContinuumMaterial3DDefGrad, IMatrixView> ghostMaterialConsMatrices;

        private Dictionary<IContinuumMaterial3DDefGrad, int> ghostMaterialsMappingToDatabase;

        private IContinuumMaterial3DDefGrad[] materialDatabase;

        private

        MaterialManager(IContinuumMaterial3DDefGrad coosenMaterial)
        {
            this.chosenMaterial = coosenMaterial;
        }

        public void AddMaterial(IContinuumMaterial3DDefGrad addedMaterial)
        {
            ghostMaterials.Add(addedMaterial);
        }

        public void UpdateMaterialStrainForRemote(IContinuumMaterial3DDefGrad ghostMaterial, double[] ghostMaterialStrain)
        {
            ghostMaterialStrains[ghostMaterial] = ghostMaterialStrain;
        }

        public double[] GetMaterialStress(IContinuumMaterial3DDefGrad ghostMaterial)
        {
            return ghostMaterialStresses[ghostMaterial];
        }

        public IMatrixView GetMaterialConstitutiveMatrix(IContinuumMaterial3DDefGrad ghostMaterial)
        {
            return ghostMaterialConsMatrices[ghostMaterial];
        }
        public void Initialize()
        {
            BuildMaterials();

            ghostMaterialStrains = ghostMaterials.Select(x => new KeyValuePair<IContinuumMaterial3DDefGrad, double[]>(x, null)).ToDictionary(x =>x.Key,x=>x.Value);
            ghostMaterialStresses = ghostMaterials.Select(x => new KeyValuePair<IContinuumMaterial3DDefGrad, double[]>(x, null)).ToDictionary(x => x.Key, x => x.Value);
            ghostMaterialConsMatrices = ghostMaterials.Select(x=>  new KeyValuePair<IContinuumMaterial3DDefGrad,IMatrixView>( x,null)).ToDictionary(x=>x.Key,x=>x.Value);

            //...
        }

        private void BuildMaterials()
        {
            materialDatabase = new IContinuumMaterial3DDefGrad[ghostMaterials.Count];
            int counter = 0;
            foreach(var material in ghostMaterials)
            {
                materialDatabase[counter] = (IContinuumMaterial3DDefGrad)chosenMaterial.Clone();
                ghostMaterialsMappingToDatabase[material] = counter; counter++;
            }
        }

        public void UpdateMaterials()
        {
            foreach(KeyValuePair<IContinuumMaterial3DDefGrad,double[]> matAndStrain  in ghostMaterialStrains)
            {
                materialDatabase[ghostMaterialsMappingToDatabase[matAndStrain.Key]].UpdateMaterial(matAndStrain.Value);
            }
        }

        public void SaveState()
        {
            foreach (KeyValuePair<IContinuumMaterial3DDefGrad, double[]> matAndStrain in ghostMaterialStrains)
            {
                materialDatabase[ghostMaterialsMappingToDatabase[matAndStrain.Key]].SaveState();
            }
        }


    }
}

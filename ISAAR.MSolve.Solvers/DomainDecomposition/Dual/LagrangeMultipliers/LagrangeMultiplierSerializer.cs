using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Transfer;

//TODO: The dictionary of subdomain nodes should be accessed by the subdomain.
namespace ISAAR.MSolve.Solvers.DomainDecomposition.Dual.LagrangeMultipliers
{
    public class LagrangeMultiplierSerializer
    {
        private readonly IDofSerializer dofSerializer;

        public LagrangeMultiplierSerializer(IDofSerializer dofSerializer)
        {
            this.dofSerializer = dofSerializer;
        }

        public (int numGlobalLagranges, List<SubdomainLagrangeMultiplier> subdomainLagranges) Deserialize(
            int[] serializedLagranges, ISubdomain subdomain, Dictionary<int, INode> subdomainNodes)
        {
            CheckSerializedLength(serializedLagranges);
            int numGlobalLagranges = serializedLagranges.Length / 4;
            var subdomainLagranges = new List<SubdomainLagrangeMultiplier>();
            for (int i = 0; i < numGlobalLagranges; ++i)
            {
                int subdomainPlusID = serializedLagranges[4 * i + 2];
                int subdomainMinusID = serializedLagranges[4 * i + 3];
                int sign = 0;
                if (subdomainPlusID == subdomain.ID) sign = +1;
                if (subdomainMinusID == subdomain.ID) sign = -1;

                if (sign != 0)
                {
                    INode node = subdomainNodes[serializedLagranges[4 * i]];
                    IDofType dofType = dofSerializer.Deserialize(serializedLagranges[4 * i + 1]);
                    subdomainLagranges.Add(new SubdomainLagrangeMultiplier(i, node, dofType, sign > 0));
                }
            }
            return (numGlobalLagranges, subdomainLagranges);
        }

        //TODO: Not thrilled about having an array of DTOs with null entries in the array and the DTOs
        /// <summary>
        /// For the lagrange multipliers that are applied to the subdomain corresponding to this process, the opposite subdomain
        /// may be null. For all other lagrange multipliers, the corresponding array entries may be null. 
        /// </summary>
        /// <param name="serializedLagranges"></param>
        /// <param name="subdomain"></param>
        public LagrangeMultiplier[] DeserializeIncompletely(int[] serializedLagranges, ISubdomain subdomain, 
            Dictionary<int, INode> subdomainNodes)
        {
            CheckSerializedLength(serializedLagranges);
            int numLagranges = serializedLagranges.Length / 4;
            var lagranges = new LagrangeMultiplier[numLagranges];
            for (int i = 0; i < numLagranges; ++i)
            {
                int subdomainPlusID = serializedLagranges[4 * i + 2];
                int subdomainMinusID = serializedLagranges[4 * i + 3];
                int sign = 0;
                if (subdomainPlusID == subdomain.ID) sign = +1;
                if (subdomainMinusID == subdomain.ID) sign = -1;

                if (sign != 0)
                {
                    INode node = subdomainNodes[serializedLagranges[4 * i]];
                    IDofType dofType = dofSerializer.Deserialize(serializedLagranges[4 * i + 1]);
                    if (sign == +1) lagranges[i] = new LagrangeMultiplier(node, dofType, subdomain, null);
                    else lagranges[i] = new LagrangeMultiplier(node, dofType, null, subdomain);
                }
            }
            return lagranges;
        }

        public int[] Serialize(LagrangeMultiplier[] lagranges)
        {
            var serializedLagranges = new int[4 * lagranges.Length];
            for (int i = 0; i < lagranges.Length; ++i)
            {
                LagrangeMultiplier lagr = lagranges[i];
                serializedLagranges[4 * i] = lagr.Node.ID;
                serializedLagranges[4 * i + 1] = dofSerializer.Serialize(lagr.DofType);
                serializedLagranges[4 * i + 2] = lagr.SubdomainPlus.ID;
                serializedLagranges[4 * i + 3] = lagr.SubdomainMinus.ID;
            }
            return serializedLagranges;
        }

        [Conditional("DEBUG")]
        private void CheckSerializedLength(int[] serializedLagranges)
        {
            if (serializedLagranges.Length % 4 != 0)
            {
                throw new ArgumentException("The provided int[] array of serialized lagrange multipliers is not valid."
                + " The array's length must be divisible by 4: element 0 = node, element 1 = dof type,"
                + " element 2 = plus subdomain, element 3 = minus subdomain.");
            }
        }
    }
}

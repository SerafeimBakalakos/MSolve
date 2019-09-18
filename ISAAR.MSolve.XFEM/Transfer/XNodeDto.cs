using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer
{
    [Serializable]
    public class XNodeDto //TODO: Enrichments
    {
        public int id;

        /// <summary>
        /// Transfering all the subdomain IDs is both time consuming and will cause problems since not all subdomains are 
        /// available to each process.
        /// </summary>
        public int multiplicity; //TODO: Storing and working with a list of boundary nodes of each subdomain would be cleaner.

        public double x, y, z;

        
        public int[] enrichmentIDs; 

        public XNodeDto(XNode node, EnrichmentSerializer enrichmentSerializer)
        {
            this.id = node.ID;
            this.multiplicity = node.Multiplicity;
            this.x = node.X;
            this.y = node.Y;
            this.z = node.Z;

            this.enrichmentIDs = new int[node.EnrichmentItems.Count];
            int counter = 0;
            foreach (IEnrichmentItem2D enrichment in node.EnrichmentItems.Keys)
            {
                enrichmentIDs[counter] = enrichmentSerializer.GetEnrichmentID(enrichment);
                ++counter;
            }
        }

        public XNode Deserialize(EnrichmentSerializer enrichmentSerializer)
        {
            var node = new XNode(id, x, y, z, multiplicity);
            foreach (int id in enrichmentIDs)
            {
                //It will be calculated later. For now I only identify that the node is enriched.
                node.EnrichmentItems[enrichmentSerializer.GetEnrichment(id)] = null;

                //TODO: Instead of this, I could transfer the nodes and their level sets to other processes and then identify
                //      there if they need to be enriched. The identification and calculation of the enrichments will be done
                //      once in master and then once in other processes. However each process will only deal with the nodes of  
                //      its subdomain. As I do it now, only the identification is avoided. Recalculating them is necessary.
            }
            return node;
        }
    }
}

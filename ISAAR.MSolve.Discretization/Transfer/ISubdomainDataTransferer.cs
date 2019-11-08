using System;
using System.Collections.Generic;
using System.Text;

//TODO: various strategies, which should probably be provided as distinct methods or different implementations of the same interface: 
//  1) send each data of each subdomain one after the other (even to the same process), pack data of the same cluster and them to 
//  each process, put data of all subdomains (and all clusters) in a single array and scatter it, etc.
//  2) Pack and unpack the data, using a dedicated class/interface or just delegates
//  3) only send/receive data of some subdomains, use a dedicated object to easily query which subdomains
//  4) use asynchronous send/receive or one thread to wait for transfering, while another goes on packing/unpacking data, with 
//     respect to the total available RAM.
//  5) Constrol serialization/deserialization and combine it with packing/unpacking
//TODO: If the data is an array of primitives, then serialization/deserialization can be avoided. Perhaps a dedicated class is 
//      needed for that, or even better dedicated methods in this interface
namespace ISAAR.MSolve.Discretization.Transfer
{
    //TODO: Not sure that the subdomainID is needed
    public delegate TPacked PackSubdomainData<TRaw, TPacked>(int subdomainID, TRaw originalData);
    public delegate TRaw UnpackSubdomainData<TRaw, TPacked>(int subdomainID, TPacked packedData);

    public interface ISubdomainDataTransferer
    {
        /// <summary>
        /// This method returns null in master process. For other processes, it returns a Dictionary with the data for each 
        /// associated subdomain.
        /// </summary>
        Dictionary<int, T> ScatterToAllSubdomains<T>(Dictionary<int, T> allSubdomainsData_master);

        /// <summary>
        /// This method returns null in master process. For other processes, it returns a Dictionary with the data for each 
        /// associated subdomain.
        /// </summary>
        Dictionary<int, TRaw> ScatterToAllSubdomainsPacked<TRaw, TPacked>(
            Dictionary<int, TRaw> allSubdomainsData_master,
            PackSubdomainData<TRaw, TPacked> packData, UnpackSubdomainData<TRaw, TPacked> unpackData);
    }
}

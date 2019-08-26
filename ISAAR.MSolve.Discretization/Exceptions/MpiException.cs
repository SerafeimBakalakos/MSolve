using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Transfer;

namespace ISAAR.MSolve.Discretization.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a method or property that is invalid for a particular process is called for that 
    /// process.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    public class MpiException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MpiException"/> class.
        /// </summary>
        public MpiException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MpiException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public MpiException(string message) : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MpiException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If the innerException parameter is not 
        ///     a null reference, the current exception is raised in a catch block that handles the inner exception. </param>
        public MpiException(string message, Exception inner) : base(message, inner)
        { }

        [Conditional("DEBUG")]
        public static void CheckProcessIsMaster(ProcessDistribution procs)
        {
            if (!procs.IsMasterProcess) throw new MpiException(
                $"Process {procs.OwnRank}: Only defined for master process (rank = {procs.MasterProcess})");
        }

        [Conditional("DEBUG")]
        public static void CheckProcessMatchesSubdomain(ProcessDistribution procs, int subdomainID)
        {
            if (subdomainID != procs.OwnSubdomainID) throw new MpiException(
                $"Process {procs.OwnRank}: This process does not have access to subdomain {subdomainID}");
        }

        [Conditional("DEBUG")]
        public static void CheckProcessMatchesSubdomainUnlessMaster(ProcessDistribution procs, int subdomainID)
        {
            if (procs.IsMasterProcess) return;
            if (subdomainID != procs.OwnSubdomainID) throw new MpiException(
                $"Process {procs.OwnRank}: This process does not have access to subdomain {subdomainID}");
        }
    }
}

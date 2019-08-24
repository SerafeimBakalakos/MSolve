using System;
using System.Collections.Generic;

namespace ISAAR.MSolve.Discretization.Interfaces
{
    public interface INode : IComparable<INode>
    {
		int ID { get; }
		double X { get; }
		double Y { get; }
		double Z { get; }

        List<Constraint> Constraints { get; }
        int Multiplicity { get; }

        //TODO: This should be implemented like in Model: NumSubdomains, EnumerateSubdomains(), GetSubdomain() plus whatever else is needed.
        Dictionary<int, ISubdomain> SubdomainsDictionary { get; }
    }
}

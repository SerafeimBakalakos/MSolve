using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.DDM.Environments;

namespace MGroup.Tests.DDM
{
	public enum EnvironmentChoice
	{
		ManagedSeqSubSingleClus, ManagedParSubSingleClus
	}

	public static class Environments
	{
		public static IProcessingEnvironment Create(this EnvironmentChoice choice)
		{
			if (choice == EnvironmentChoice.ManagedSeqSubSingleClus)
			{
				return new ProcessingEnvironment(
					new SubdomainEnvironmentManagedSequential(), new ClusterEnvironmentManagedSequential());
			}
			else if (choice == EnvironmentChoice.ManagedParSubSingleClus)
			{
				return new ProcessingEnvironment(
					new SubdomainEnvironmentManagedParallel(), new ClusterEnvironmentManagedSequential());
			}
			else
			{
				throw new NotImplementedException("This request cannot be satisfied yet.");
			}
		}
	}
}

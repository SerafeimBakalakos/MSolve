using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;

//TODO: DofTypes should be enums. XFEM should find a different way for its enriched dofs. Another approach is for IDofType to 
//		have an ID property
namespace MGroup.Solvers.DDM.Dofs
{
	public static class AllDofs
	{
		private static readonly Dictionary<int, IDofType> codesToDofs = new Dictionary<int, IDofType>();
		private static readonly Dictionary<IDofType, int> dofsToCodes = new Dictionary<IDofType, int>();
		private static readonly object insertionLock = new object();
		private static int nextCode = 0;

		public static void AddDof(IDofType dof)
		{
			lock (insertionLock)
			{
				bool exists = dofsToCodes.ContainsKey(dof);
				if (!exists)
				{
					dofsToCodes[dof] = nextCode;
					codesToDofs[nextCode] = dof;
					++nextCode;
				}
			}
		}

		public static void AddStructuralDofs()
		{
			AddDof(StructuralDof.TranslationX);
			AddDof(StructuralDof.TranslationY);
			AddDof(StructuralDof.TranslationZ);
			AddDof(StructuralDof.RotationX);
			AddDof(StructuralDof.RotationY);
			AddDof(StructuralDof.RotationZ);
		}

		public static int GetCodeOfDof(IDofType dof) => dofsToCodes[dof];

		public static IDofType GetDofOfCode(int code) => codesToDofs[code];
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Solvers.Interfaces;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Logging;
using ISAAR.MSolve.Logging.Interfaces;
using System.Diagnostics;
using ISAAR.MSolve.Solvers.Commons;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: Optimization: I could avoid initialization and GC of some vectors by reusing existing ones.
namespace ISAAR.MSolve.Analyzers
{
    public class NewmarkDynamicAnalyzer_v2 : IAnalyzer_v2, INonLinearParentAnalyzer_v2
    {
        private readonly double alpha, delta, timeStep, totalTime;
        private double a0, a1, a2, a3, a4, a5, a6, a7;//, a2a0, a3a0, a4a1, a5a1;
        private Dictionary<int, Vector> rhs = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> uu = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> uum = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> uc = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> ucc = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> u = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> v = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> v1 = new Dictionary<int, Vector>();
        private Dictionary<int, Vector> v2 = new Dictionary<int, Vector>();
        //private readonly Dictionary<int, IImplicitIntegrationAnalyzerLog> resultStorages =
        //    new Dictionary<int, IImplicitIntegrationAnalyzerLog>();
        private readonly Dictionary<int, ImplicitIntegrationAnalyzerLog> resultStorages =
            new Dictionary<int, ImplicitIntegrationAnalyzerLog>();
        private readonly IDictionary<int, ILinearSystem_v2> subdomains;
        private readonly IImplicitIntegrationProvider_v2 provider;
        private IAnalyzer_v2 childAnalyzer;
        private IAnalyzer_v2 parentAnalyzer;

        public NewmarkDynamicAnalyzer_v2(IImplicitIntegrationProvider_v2 provider, IAnalyzer_v2 embeddedAnalyzer, 
            IDictionary<int, ILinearSystem_v2> subdomains,
            double alpha, double delta, double timeStep, double totalTime)
        {
            this.provider = provider;
            this.childAnalyzer = embeddedAnalyzer;
            this.subdomains = subdomains;
            this.alpha = alpha;
            this.delta = delta;
            this.timeStep = timeStep;
            this.totalTime = totalTime;
            this.childAnalyzer.ParentAnalyzer = this;
        }

        public Dictionary<int, ImplicitIntegrationAnalyzerLog> ResultStorages { get { return resultStorages; } }

        private void InitializeCoefficients()
        {
            if (delta < 0.5) throw new InvalidOperationException("Newmark delta has to be bigger than 0.5.");
            double aLimit = 0.25 * Math.Pow(0.5 + delta, 2);
            if (alpha < aLimit) throw new InvalidOperationException("Newmark alpha has to be bigger than " + aLimit.ToString() + ".");

            a0 = 1 / (alpha * timeStep * timeStep);
            a1 = delta / (alpha * timeStep);
            a2 = 1 / (alpha * timeStep);
            a3 = 1 / (2 * alpha) - 1;
            a4 = delta / alpha - 1;
            a5 = timeStep * 0.5 * (delta / alpha - 2);
            a6 = timeStep * (1 - delta);
            a7 = delta * timeStep;
        }

        //TODO: Remove this. The analyzer should not mess with the solution vector.
        //public void ResetSolutionVectors()
        //{
        //    foreach (ILinearSystem_v2 subdomain in subdomains.Values)
        //        subdomain.Solution.Clear();
        //}

        private void InitializeInternalVectors()
        {
            uu.Clear();
            uum.Clear();
            uc.Clear();
            ucc.Clear();
            u.Clear();
            v.Clear();
            v1.Clear();
            v2.Clear();
            rhs.Clear();

            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
            {
                int dofs = subdomain.RhsVector.Length;
                uu.Add(subdomain.ID, Vector.CreateZero(dofs));
                uum.Add(subdomain.ID, Vector.CreateZero(dofs));
                uc.Add(subdomain.ID, Vector.CreateZero(dofs));
                ucc.Add(subdomain.ID, Vector.CreateZero(dofs));
                u.Add(subdomain.ID, Vector.CreateZero(dofs));
                v.Add(subdomain.ID, Vector.CreateZero(dofs));
                v1.Add(subdomain.ID, Vector.CreateZero(dofs));
                v2.Add(subdomain.ID, Vector.CreateZero(dofs));
                rhs.Add(subdomain.ID, Vector.CreateZero(dofs));

                // Account for initial conditions coming from a previous solution
                v[subdomain.ID] = Vector.CreateFromVector(subdomain.Solution);
            }
        }

        private void InitializeMatrices()
        {
            ImplicitIntegrationCoefficients coeffs = new ImplicitIntegrationCoefficients
            {
                Mass = a0,
                Damping = a1,
                Stiffness = 1
            };
            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
                provider.CalculateEffectiveMatrix(subdomain, coeffs);


            //TODO: Remove the following comments and commented-out code. 
            // What is the point of the following code? Generally nonZeroCount should be queried from the matrix itself, but 
            // here it isn't even used anywhere. On the other hand, this code requires significant extra memory and calculations.
            // var m = (SkylineMatrix2D)subdomains[0].Matrix;//TODO: Subdomain matrices should not be retrieved like that.
            //var x = new HashSet<double>();
            //int nonZeroCount = 0;
            //for (int i = 0; i < m.Data.Length; i++)
            //{
            //    nonZeroCount += m.Data[i] != 0 ? 1 : 0;
            //    if (x.Contains(m.Data[i]) == false)
            //        x.Add(m.Data[i]);
            //}
            //nonZeroCount += 0;
        }

        private void InitializeRHSs()
        {
            ImplicitIntegrationCoefficients coeffs = new ImplicitIntegrationCoefficients
            {
                Mass = a0,
                Damping = a1,
                Stiffness = 1
            };
            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
            {
                provider.ProcessRHS(subdomain, coeffs);
                int dofs = subdomain.RhsVector.Length;
                rhs[subdomain.ID] = Vector.CreateFromVector(subdomain.RhsVector); //TODO: copying the vectors is wasteful.
            }
        }

        private void UpdateResultStorages(DateTime start, DateTime end)
        {
            if (childAnalyzer == null) return;
            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
                if (resultStorages.ContainsKey(subdomain.ID))
                    if (resultStorages[subdomain.ID] != null)
                        foreach (var l in childAnalyzer.Logs[subdomain.ID])
                            resultStorages[subdomain.ID].StoreResults(start, end, l);
        }

        #region IAnalyzer Members

        public Dictionary<int, IAnalyzerLog[]> Logs { get { return null; } } 

        public IAnalyzer_v2 ParentAnalyzer
        {
            get { return parentAnalyzer; }
            set { parentAnalyzer = value; }
        }

        public IAnalyzer_v2 ChildAnalyzer
        {
            get { return childAnalyzer; }
            set { childAnalyzer = value; }
        }

        public void Initialize()
        {
            if (childAnalyzer == null) throw new InvalidOperationException("Static analyzer must contain an embedded analyzer.");
            //InitializeCoefficients();
            InitializeInternalVectors();
            //InitializeMatrices();
            InitializeRHSs();
            childAnalyzer.Initialize();
        }

        private Vector CalculateRHSImplicit(ILinearSystem_v2 subdomain, bool addRHS)
        {
            //TODO: instead of creating a new Vector and then trying to set ILinearSystem.RhsVector, clear it and operate on it.

            int id = subdomain.ID;

            // uu = a0 * v + a2 * v1 + a3 * v2
            uu[id] = v[id].LinearCombination(a0, v1[id], a2);
            uu[id].AxpyIntoThis(v2[id], a3);

            // uc = a1 * v + a4 * v1 + a5 * v2
            uc[id] = v[id].LinearCombination(a1, v1[id], a4);
            uc[id].AxpyIntoThis(v2[id], a5);

            uum[id] = provider.MassMatrixVectorProduct(subdomain, uu[id]);
            ucc[id] = provider.DampingMatrixVectorProduct(subdomain, uc[id]);

            Vector rhsResult = uum[id] + ucc[id];
            if (addRHS) rhsResult.AddIntoThis(rhs[id]);
            return rhsResult;
        }

        private void CalculateRHSImplicit()
        {
            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
            {
                //int id = subdomain.ID;
                //for (int i = 0; i < subdomain.RHS.Length; i++)
                //    subdomain.RHS[i] = rhs[id].Data[i];

                subdomain.RhsVector = CalculateRHSImplicit(subdomain, true); 

                //int id = subdomain.ID;
                //for (int i = 0; i < subdomain.RHS.Length; i++)
                //{
                //    //uu[id].Data[i] = v[id].Data[i] + a2a0 * v1[id].Data[i] + a3a0 * v2[id].Data[i];
                //    uu[id].Data[i] = a0 * v[id].Data[i] + a2 * v1[id].Data[i] + a3 * v2[id].Data[i];
                //    uc[id].Data[i] = a1 * v[id].Data[i] + a4 * v1[id].Data[i] + a5 * v2[id].Data[i];
                //}
                ////provider.Ms[id].Multiply(uu[id], uum[id].Data);
                //provider.MassMatrixVectorProduct(subdomain, uu[id], uum[id].Data);
                //provider.DampingMatrixVectorProduct(subdomain, uc[id], ucc[id].Data);
                //for (int i = 0; i < subdomain.RHS.Length; i++)
                //    subdomain.RHS[i] = rhs[id].Data[i] + uum[id].Data[i] + ucc[id].Data[i];
            }
            #region Old Fortran code
            //If (ptAnal.tDynamic.bHasDamping) Then
            //    atDynUU(iMesh).afValues = atDynV(iMesh).afValues + fA2A0 * atDynV1(iMesh).afValues + fA3A0 * atDynV2(iMesh).afValues
            //    atDynUC(iMesh).afValues = atDynV(iMesh).afValues + fA4A1 * atDynV1(iMesh).afValues + fA5A1 * atDynV2(iMesh).afValues
            //Else
            //    atDynUU(iMesh).afValues = atDynV(iMesh).afValues + fA2A0 * atDynV1(iMesh).afValues + fA3A0 * atDynV2(iMesh).afValues
            //End If
            //If (Present(ProcessVel)) Then
            //    Call ProcessVel(iMesh, Size(atDynUU(iMesh).afValues), atDynUU(iMesh).afValues, atDynUUM(iMesh).afValues)
            //Else
            //    Call MatrixVectorMultiplication(iMesh, Size(atRHS(iMesh).afValues), 2, atDynUU(iMesh).afValues, atDynUUM(iMesh).afValues)
            //End If
            //If (ptAnal.tDynamic.bHasDamping) Then
            //    If (Present(ProcessAcc)) Then
            //        Call ProcessAcc(iMesh, Size(atDynUU(iMesh).afValues), atDynUC(iMesh).afValues, atDynUCC(iMesh).afValues)
            //    Else
            //        Call MatrixVectorMultiplication(iMesh, Size(atRHS(iMesh).afValues), 3, atDynUC(iMesh).afValues, atDynUCC(iMesh).afValues)
            //    End If
            //    atRHS(iMesh).afValues = atRHS(iMesh).afValues + atDynUUM(iMesh).afValues + atDynUCC(iMesh).afValues
            //Else
            //    atRHS(iMesh).afValues = atRHS(iMesh).afValues + atDynUUM(iMesh).afValues
            //End If
            #endregion
        }

        public void Solve()
        {
            if (childAnalyzer == null) throw new InvalidOperationException(
                "NewmarkDynamicAnalyzer analyzer must contain an embedded analyzer.");

            for (int i = 0; i < (int)(totalTime / timeStep); i++)
            {
                Debug.WriteLine("Newmark step: {0}", i);
                provider.GetRHSFromHistoryLoad(i);
                InitializeRHSs();
                // ProcessRHS
                CalculateRHSImplicit();
                DateTime start = DateTime.Now;
                childAnalyzer.Solve();
                DateTime end = DateTime.Now;
                UpdateVelocityAndAcceleration(i);
                UpdateResultStorages(start, end);
            }

            //childAnalyzer.Solve();
        }

        private void UpdateVelocityAndAcceleration(int timeStep)
        {
            var externalVelocities = provider.GetVelocitiesOfTimeStep(timeStep);
            var externalAccelerations = provider.GetAccelerationsOfTimeStep(timeStep);

            foreach (ILinearSystem_v2 subdomain in subdomains.Values)
            {
                int id = subdomain.ID;
                u[id].CopyFrom(v[id]); //TODO: this copy can be avoided by pointing to v[id] and then v[id] = null;
                v[id].CopyFrom(subdomain.Solution);

                Vector vv = v2[id] + externalAccelerations[id];

                // v2 = a0 * (v - u) - a2 * v1 - a3 * vv
                v2[id] = v[id] - u[id];
                v2[id].LinearCombinationIntoThis(a0, v1[id], -a2);
                v2[id].AxpyIntoThis(vv, -a3);

                // v1 = v1 + externalVelocities + a6 * vv + a7 * v2
                v1[id].AddIntoThis(externalVelocities[id]);
                v1[id].AxpyIntoThis(vv, a6);
                v1[id].AxpyIntoThis(v2[id], a7);
            }
        }

        public void BuildMatrices()
        {
            InitializeCoefficients();
            InitializeMatrices();
        }

        #endregion

        #region INonLinearParentAnalyzer Members

        public IVector GetOtherRHSComponents(ILinearSystem_v2 subdomain, IVector currentSolution)
        {
            //// u[id]: old solution
            //// v[id]: current solution
            //// vv: old acceleration
            //// v2: current acceleration
            //// v1: current velocity
            ////double vv = v2[id].Data[j];
            ////v2[id].Data[j] = a0 * (v[id].Data[j] - u[id].Data[j]) - a2 * v1[id].Data[j] - a3 * vv;
            ////v1[id].Data[j] += a6 * vv + a7 * v2[id].Data[j];

            //int id = subdomain.ID;
            //Vector<double> currentAcceleration = new Vector<double>(subdomain.Solution.Length);
            //Vector<double> currentVelocity = new Vector<double>(subdomain.Solution.Length);
            //Vector<double> uu = new Vector<double>(subdomain.Solution.Length);
            //Vector<double> uc = new Vector<double>(subdomain.Solution.Length);
            //for (int j = 0; j < subdomain.RHS.Length; j++)
            //{
            //    currentAcceleration.Data[j] = a0 * (currentSolution[j] - v[id].Data[j]) - a2 * v1[id].Data[j] - a3 * v2[id].Data[j];
            //    currentVelocity.Data[j] = v1[id].Data[j] + a6 * v2[id].Data[j] + a7 * currentAcceleration.Data[j];
            //    uu.Data[j] = a0 * currentSolution[j] + a2 * currentVelocity.Data[j] + a3 * currentAcceleration.Data[j];
            //    uc.Data[j] = a1 * currentSolution[j] + a4 * currentVelocity.Data[j] + a5 * currentAcceleration.Data[j];
            //}

            //Vector<double> tempResult = new Vector<double>(subdomain.Solution.Length);
            //Vector<double> result = new Vector<double>(subdomain.Solution.Length);
            //provider.MassMatrixVectorProduct(subdomain, uu, tempResult.Data);
            //result.Add(tempResult);

            //provider.DampingMatrixVectorProduct(subdomain, uc, tempResult.Data);
            //result.Add(tempResult);

            //return result.Data;

            //Vector<double> uu = new Vector<double>(subdomain.Solution.Length);
            //Vector<double> uc = new Vector<double>(subdomain.Solution.Length);
            //int id = subdomain.ID;
            //for (int j = 0; j < subdomain.RHS.Length; j++)
            //{
            //    uu.Data[j] = -a0 * (v[id].Data[j] - currentSolution[j]) - a2 * v1[id].Data[j] - a3 * v2[id].Data[j];
            //    uc.Data[j] = -a1 * (v[id].Data[j] - currentSolution[j]) - a4 * v1[id].Data[j] - a5 * v2[id].Data[j];
            //}
            //provider.MassMatrixVectorProduct(subdomain, uu, tempResult.Data);
            //result.Add(tempResult);
            //provider.DampingMatrixVectorProduct(subdomain, uc, tempResult.Data);
            //result.Add(tempResult);

            ////CalculateRHSImplicit(subdomain, result.Data, false);
            ////result.Scale(-1d);

            // result = a0 * (M * u) + a1 * (C * u) 
            Vector result = provider.MassMatrixVectorProduct(subdomain, currentSolution);
            Vector temp = provider.DampingMatrixVectorProduct(subdomain, currentSolution);
            result.LinearCombinationIntoThis(a0, temp, a1);
            return result;
        }

        #endregion
    }
}
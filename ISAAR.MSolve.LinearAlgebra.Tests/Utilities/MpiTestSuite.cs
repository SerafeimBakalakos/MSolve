using System;
using System.Collections.Generic;
using System.Text;
using MPI;

//TODO: Needs printing the stack trace when a test fails. ASAP.
//TODO: Avoid forcing the user to pass the class and method names. At least find a better way to pass the method name.
//TODO: Perhaps the static method should be moved to Solvers.Tests.Program.Main(string[] args) instead of being here and called
//      by SamplesConsole.Program.Main(string[] args).
namespace ISAAR.MSolve.LinearAlgebra.Tests.Utilities
{
    public class MpiTestSuite
    {
        private readonly List<(Action test, string className, string methodName)> tests =
            new List<(Action test, string className, string methodName)>();

        public void AddFact(Action test, string className, string methodName) => tests.Add((test, className, methodName));

        public void AddTheory<TInput>(Action<TInput> test, string className, string methodName, TInput input)
            => tests.Add(( () => test(input), className, $"{methodName}(args = {input})" ));

        public void AddTheory<TInput0, TInput1>(Action<TInput0, TInput1> test,
            string className, string methodName, TInput0 input0, TInput1 input1)
        {
            tests.Add((() => test(input0, input1), className, $"{methodName}(args = {input0}, {input1})"));
        }

        public void AddTheory<TInput0, TInput1, TInput2>(Action<TInput0, TInput1, TInput2> test,
            string className, string methodName, TInput0 input0, TInput1 input1, TInput2 input2)
        {
            tests.Add((() => test(input0, input1, input2), className,
                $"{methodName}(args = {input0}, {input1}, {input2})"));
        }

        public void AddTheory<TInput0, TInput1, TInput2, TInput3>(Action<TInput0, TInput1, TInput2, TInput3> test, 
            string className, string methodName, TInput0 input0, TInput1 input1, TInput2 input2, TInput3 input3)
        {
            tests.Add((() => test(input0, input1, input2, input3), className, 
                $"{methodName}(args = {input0}, {input1}, {input2}, {input3})"));
        }

        public void AddTheory<TInput0, TInput1, TInput2, TInput3, TInput4>(
            Action<TInput0, TInput1, TInput2, TInput3, TInput4> test,
            string className, string methodName, TInput0 input0, TInput1 input1, TInput2 input2, TInput3 input3, TInput4 input4)
        {
            tests.Add((() => test(input0, input1, input2, input3, input4), className,
                $"{methodName}(args = {input0}, {input1}, {input2}, {input3}, {input4})"));
        }

        public void RunTests(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                string header = $"Process {comm.Rank}: ";

                Console.WriteLine(header + "Starting running tests.");
                for (int t = 0; t < tests.Count; ++t)
                {
                    (Action test, string className, string methodName) = tests[t];
                    comm.Barrier();
                    try
                    {
                        test();
                        Console.WriteLine(header + $"Test {t} - {className}.{methodName} passed!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(header + $"Test {t} - {className}.{methodName} failed! \n" + ex.StackTrace);
                    }
                }
                comm.Barrier();
                Console.WriteLine(header + "All tests were completed.");
            }
        }
    }
}

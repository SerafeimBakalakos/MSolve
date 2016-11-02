namespace ISAAR.MSolve.Analyzers.Optimization.Problem
{
    public interface IDesign
    {
        double[] ObjectiveValues { get; }
        double[] ConstraintValues { get; }
    }

    public interface IDesignFactory
    {
        IDesign CreateDesign(double[] x);
    }
}
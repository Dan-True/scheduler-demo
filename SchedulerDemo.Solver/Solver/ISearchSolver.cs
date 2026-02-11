namespace SchedulerDemo.Solver.Solver.Solver
{
    public interface ISearchSolver
    {
        Task<SolveResult> SolveAsync(SolveRequest request, CancellationToken token);
    }
}

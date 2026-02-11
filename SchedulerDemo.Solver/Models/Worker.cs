namespace SchedulerDemo.Solver.Models
{
    public record Worker(string Id, string Name, IReadOnlySet<string> Skills);
}

using SchedulerDemo.Solver.Models;

namespace SchedulerDemo.DTO
{
    public class WorkerDTO
    {
        public required string WorkerId { get; init; }
        public required string Name { get; init; }
        public HashSet<string> Skills { get; init; }

        public Worker MapToWorker()
        {
            return new Worker(WorkerId, Name, Skills);
        }
    }
}

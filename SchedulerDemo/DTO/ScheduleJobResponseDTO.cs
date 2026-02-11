namespace SchedulerDemo.DTO
{
    public class ScheduleJobResponseDTO
    {
        public required Guid JobId { get; init; }

        public required string Location { get; init; }
    }
}

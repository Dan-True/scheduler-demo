namespace SchedulerDemo.DTO
{
    public class AssignmentDTO
    {
        public DateOnly Date { get; set; }

        public ShiftTypeDTO Shift { get; set; }
        
        public IReadOnlyList<string> WorkerIds { get; set; }
    }
}

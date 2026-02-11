using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using SchedulerDemo.JobWorker;
using SchedulerDemo.Options;
using SchedulerDemo.Scheduler.JobStore;
using SchedulerDemo.Scheduler.Queue;
using SchedulerDemo.Solver.Rules;
using SchedulerDemo.Solver.Skills;
using SchedulerDemo.Solver.Solver;
using SchedulerDemo.Solver.Solver.Solver;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());

        // Set up configuration options
        builder.Services.Configure<SchedulingQueueOptions>(
            builder.Configuration.GetSection("SchedulingOptions"));


        // Set up rest of DI
        builder.Services.AddSingleton<ISchedulingQueue>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SchedulingQueueOptions>>().Value;
            return new SchedulingQueue(options.Capacity);
        });

        builder.Services.AddSingleton<IJobStore, InMemoryJobStore>();
        builder.Services.AddHostedService<ScheduleJobWorker>();

        builder.Services.AddSingleton<IRuleEngine, NoRulesEngine>();
        builder.Services.AddSingleton<ISkillMatcher, NoSkillMatcher>();
        builder.Services.AddSingleton<ISearchSolver, SearchSolver>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SchedulerDemo API v1");
                options.RoutePrefix = ""; // Since we have no UI, just serve Swagger at root
            });
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
using ADOMigration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Reporting;

public class ExecutionReporter
{
    private string FilePath { get; }

    public ExecutionReporter(string reportingDirectory)
    {
        FilePath = Path.Combine(reportingDirectory, $"Execution_Report_{DateTime.Now}.csv");

        using StreamWriter writer = File.AppendText(FilePath);
        writer.WriteLine("workItemId, workItemType, iterationPath, areaPath");
    }

    public void Add(WorkItem workItem)
    {
        try
        {
            using StreamWriter writer = File.AppendText(FilePath);
            writer.WriteLine($"{workItem.Id}, {workItem.WorkItemType()}, {workItem.IterationPath()}, {workItem.AreaPath()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while reporting for workitem {workItem.Id} Exception: {ex.Message}");
            throw;
        }
    }
}

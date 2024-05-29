using ADOMigration.Extensions;
using ADOMigration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Logging;

public class ExecutionLogger
{
    private string FilePath { get; }
    private long ExecutionStep { get; set; }

    public ExecutionLogger(string loggingDirectory, long executinStep = 1)
    {
        FilePath = Path.Combine(loggingDirectory, $"Execution_Logs_{DateTime.Now}.csv");
        ExecutionStep = executinStep;

        using StreamWriter writer = File.AppendText(FilePath);
        writer.WriteLine("executionStep, workItemId, workItemType, operation, prevValue, newValue, status");
    }

    public void Log(WorkItem workItem, SystemOperation operation, string prevValue, string newValue, OperationStatus status)
    {
        try
        {
            using StreamWriter writer = File.AppendText(FilePath);

            ExecutionStep++;
            ExecutionLogEntry entry = new(ExecutionStep, workItem, operation, prevValue, newValue, status);

            writer.WriteLine(entry.ToCsv());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while writing excution logs for workitem {workItem.Id} Exception: {ex.Message}");
            throw;
        }
    }
}

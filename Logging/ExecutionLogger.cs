﻿using ADOMigration.Model;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Logging;

public class ExecutionLogger
{
    private string FilePath { get; }
    private long ExecutionStep { get; set; }
    private string delimeter { get; set; } = "`";

    public ExecutionLogger(string loggingDirectory, long executinStep = 1)
    {
        FilePath = Path.Combine(loggingDirectory, $"Execution_Logs_{DateTime.Now:ddMMyyyyHHMMss}.txt");
        ExecutionStep = executinStep;

        using StreamWriter writer = File.AppendText(FilePath);
        writer.WriteLine($"executionStep{delimeter}workItemId{delimeter}workItemType{delimeter}operation{delimeter}prevValue{delimeter}newValue{delimeter}status");
    }

    public void Log(WorkItem workItem, SystemOperation operation, string prevValue, string newValue, OperationStatus status)
    {
        try
        {
            using StreamWriter writer = File.AppendText(FilePath);

            ExecutionLogEntry entry = new(ExecutionStep, workItem, operation, prevValue, newValue, status);
            ExecutionStep++;

            writer.WriteLine(entry.ToCsv(delimeter));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while writing excution logs for workitem {workItem.Id} Exception: {ex.Message}");
            throw;
        }
    }
}

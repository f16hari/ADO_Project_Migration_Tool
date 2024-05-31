using ADOMigration.Extensions;
using ADOMigration.Model;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration;

public class ExecutionLogEntry
{
    public long ExecutionStep { get; set; }
    public int WorkItemId { get; set; }
    public string WorkItemType { get; set; }
    public SystemOperation Operation { get; set; }
    public string PrevValue { get; set; }
    public string NewValue { get; set; }
    public OperationStatus Status { get; set; }

    public ExecutionLogEntry(long executionStep, WorkItem workItem, SystemOperation operation, string prevValue, string newValue, OperationStatus status)
    {
        ExecutionStep = executionStep;
        WorkItemId = workItem.Id ?? 0;
        WorkItemType = workItem.Type();
        Operation = operation;
        PrevValue = prevValue;
        NewValue = newValue;
        Status = status;
    }

    public ExecutionLogEntry(string logEntry, string delimeter = ",")
    {
        var splits = logEntry.Split(delimeter);

        ExecutionStep = long.Parse(splits[0]);
        WorkItemId = int.Parse(splits[1]);
        WorkItemType = splits[2];
        Operation = (SystemOperation)Enum.Parse(typeof(SystemOperation), splits[3]);
        PrevValue = splits[4];
        NewValue = splits[5];
        Status = (OperationStatus)Enum.Parse(typeof(OperationStatus), splits[6]);
    }

    public string ToCsv(string delimeter = ",")
    {
        return $"{ExecutionStep}{delimeter}{WorkItemId}{delimeter}{WorkItemType}{delimeter}{Operation}{delimeter}{PrevValue}{delimeter}{NewValue}{delimeter}{Status}";
    }
}

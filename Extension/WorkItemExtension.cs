using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Extensions;

public static class WorkItemExtension
{
    public static string WorkItemType(this WorkItem workItem)
    {
        return workItem.Fields.First(x => x.Key == "System.WorkItemType").Value.ToString() ?? string.Empty;
    }

    public static string TeamProject(this WorkItem workItem)
    {
        return workItem.Fields.First(x => x.Key == "System.TeamProject").Value.ToString() ?? string.Empty;
    }

    public static string AreaPath(this WorkItem workItem)
    {
        return workItem.Fields.First(x => x.Key == "System.AreaPath").Value.ToString() ?? string.Empty;
    }

    public static string IterationPath(this WorkItem workItem)
    {
        return workItem.Fields.First(x => x.Key == "System.IterationPath").Value.ToString() ?? string.Empty;
    }

    public static bool IsTestArtifact(this WorkItem workItem)
    {
        var workItemType = workItem.WorkItemType();

        return workItemType == "Test Cause" || workItemType == "Test Suite" || workItemType == "Test Plan";
    }
}

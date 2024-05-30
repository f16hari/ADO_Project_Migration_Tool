using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Utilty;

public class WorkItemTraverser(WorkItemTrackingHttpClient witClient)
{
    public HashSet<int> VisitedWorkItems { get; set; } = [];

    public void Traverse(int workItemId, Func<int, bool> shouldProcess, Action<int> callBack)
    {
        try
        {
            VisitedWorkItems.Add(workItemId);
            var workItem = witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

            if (workItem.Relations == null) return;

            foreach (var relatedWorkItem in workItem.Relations)
            {
                if (!int.TryParse(relatedWorkItem.Url.Split('/').Last(), out var relatedWorkItemId)
                    || VisitedWorkItems.Contains(relatedWorkItemId)
                    || !shouldProcess(relatedWorkItemId)) continue;

                callBack.Invoke(relatedWorkItemId);
                Traverse(relatedWorkItemId, shouldProcess, callBack);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to traverse workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }
}

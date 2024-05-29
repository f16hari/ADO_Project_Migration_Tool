using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Utilty;

public class WorkItemTraverser(WorkItemTrackingHttpClient witClient)
{
    public void Traverse(int workItemId, Func<int, bool> shouldProcess, Action<int> callBack)
    {
        try
        {
            var workItem = witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

            foreach (var relatedWorkItem in workItem.Relations)
            {
                var relatedWorkItemId = int.Parse(relatedWorkItem.Url.Split('/').Last());

                if(!shouldProcess(relatedWorkItemId)) return;

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

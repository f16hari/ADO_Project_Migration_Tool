using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Utilty;

public class WorkItemFetcher(WorkItemTrackingHttpClient witClient)
{
    public List<WorkItem> FetchByWITQuery(string query)
    {
        List<WorkItem> workItems = [];
        var wiql = new Wiql
        {
            Query = query
        };

        var result = witClient.QueryByWiqlAsync(wiql).Result;
        int skip = 0;
        const int batchSize = 100;
        IEnumerable<WorkItemReference> workItemRefs;
        do
        {
            workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);

            if (workItemRefs.Any())
            {
                workItems.AddRange(witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id), expand: WorkItemExpand.Relations).Result);
            }

            skip += batchSize;
        } while (workItemRefs.Count() == batchSize);

        return workItems;
    }

    public List<WorkItem> FetchByIds(string project, string idsAsString)
    {
        var ids = idsAsString.Split(",").ToList();
        List<WorkItem> workItems = [];

        int skip = 0;
        const int batchSize = 100;
        do
        {
            var workItemRefs = ids.Skip(skip).Take(batchSize);

            if (workItemRefs.Any())
            {
                workItems.AddRange(witClient.GetWorkItemsAsync(project, workItemRefs.Select(int.Parse), expand: WorkItemExpand.Relations).Result);
            }

            skip += batchSize;
        } while (workItems.Count == batchSize);

        return workItems;
    }
}

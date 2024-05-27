using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADOMigration.Utilty;

public class WorkItemFetcher
{
    private VssConnection Connection { get; set; }
    private WorkItemTrackingHttpClient WitClient { get; set; }
    private ILogger Logger { get; set; }

    public WorkItemFetcher(IConfiguration config, ILogger logger)
    {
        var orgUrl = new Uri(config.GetValue<string>("Migration:OrgURL") ?? string.Empty);
        var pat = config.GetValue<string>("Migration:PersonalAcessToken") ?? string.Empty;

        Connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, pat));
        WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
        Logger = logger;
    }

    public List<WorkItem> FetchByWITQuery(string query)
    {
        List<WorkItem> workItems = [];
        var wiql = new Wiql
        {
            Query = query
        };

        var result = WitClient.QueryByWiqlAsync(wiql).Result;
        if (result.WorkItems.Any())
        {
            int skip = 0;
            const int batchSize = 100;
            IEnumerable<WorkItemReference> workItemRefs;
            do
            {
                workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);

                if (workItemRefs.Any())
                {
                    workItems.AddRange(WitClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id), expand: WorkItemExpand.Relations).Result);
                }

                skip += batchSize;
            } while (workItemRefs.Count() == batchSize);
        }
        else
        {
            Logger.Log($"No workitems found!!! for the query : {query}");
        }

        return workItems;
    }

    public List<WorkItem> FetchByIds(string project, string idsAsString)
    {
        var ids = idsAsString.Split(",").ToList();
        List<WorkItem> workItems = [];

        if (ids.Count != 0)
        {
            int skip = 0;
            const int batchSize = 100;
            do
            {
                var workItemRefs = ids.Skip(skip).Take(batchSize);

                if (workItemRefs.Any())
                {
                    workItems.AddRange(WitClient.GetWorkItemsAsync(project, workItemRefs.Select(int.Parse), expand: WorkItemExpand.Relations).Result);
                }

                skip += batchSize;
            } while (workItems.Count == batchSize);
        }
        else
        {
            Logger.Log($"No workitems found!!! for the ids : {idsAsString}");
        }

        return workItems;
    }
}

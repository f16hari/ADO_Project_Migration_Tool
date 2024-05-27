using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace ADOMigration.Utilty;

public class WorkItemRelationHelper
{
    private VssConnection Connection { get; set; }
    private WorkItemTrackingHttpClient WitClient { get; set; }
    private ILogger Logger { get; set; }

    public WorkItemRelationHelper(IConfiguration config, ILogger logger)
    {
        var orgUrl = new Uri(config.GetValue<string>("Migration:OrgURL") ?? string.Empty);
        var pat = config.GetValue<string>("Migration:PersonalAcessToken") ?? string.Empty;

        Connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, pat));
        WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
        Logger = logger;
    }

    public List<Tuple<string, string>> RemoveTestArtifactRelations(int workItemId, string project)
    {
        var workItem = WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;
        
        if (workItem == null || workItem.Id == null || workItem.Relations == null) return [];

        try
        {
            Logger.Log($"Started removing Test Artifact relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");

            List<int> relationsToBreak = [];
            List<Tuple<string, string>> removedRelations = [];
            int currentIndex = -1;

            foreach (var relation in workItem.Relations)
            {
                currentIndex++;
                var relatedWorkItemId = int.Parse(relation.Url.Split('/').Last());
                WorkItem relatedWorkItem = WitClient.GetWorkItemAsync(project, relatedWorkItemId).Result;


                if (relatedWorkItem != null)
                {
                    if (relatedWorkItem.Fields.Any(x => x.Key == "System.WorkItemType" && x.Value.ToString() != "Test Case" && x.Value.ToString() != "Test Suite" && x.Value.ToString() != "Test Plan")) continue;

                    relationsToBreak.Add(currentIndex);
                    removedRelations.Add(new Tuple<string, string>(relation.Rel, relation.Url));
                }
            }

            if (relationsToBreak.Count != 0)
            {
                var patchDocument = new JsonPatchDocument();

                foreach (var relationToBreak in relationsToBreak)
                {
                    var deleteOperation = new JsonPatchOperation()
                    {
                        Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Remove,
                        Path = $"/relations/{relationToBreak}",
                    };

                    patchDocument.Add(deleteOperation);
                }

                var result = WitClient.UpdateWorkItemAsync(patchDocument, (int)workItem.Id).Result;
            }

            Logger.Log($"Completed removing {relationsToBreak.Count} Test Artifact relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");

            return removedRelations;
        }
        catch (Exception ex)
        {
            Logger.Log($"Removing Test Artifact relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value} Failed!! Exception: {ex.Message}");
            return [];
        }
    }

    public void AddRelations(int workItemId, List<Tuple<string, string>> relations)
    {
        if (relations == null || relations.Count == 0) return;

        var workItem = WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;
        if (workItem == null || workItem.Id == null) return;

        try
        {
            Logger.Log($"Adding relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");

            var patchDocument = new JsonPatchDocument();

            foreach (var relation in relations)
            {
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        Rel = relation.Item1,
                        Url = relation.Item2
                    }
                });
            }


            var result = WitClient.UpdateWorkItemAsync(patchDocument, (int)workItem.Id).Result;
            Logger.Log($"Completed adding relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Adding relations for work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value} Failed, Exception : {ex.Message}");
        }
    }
}
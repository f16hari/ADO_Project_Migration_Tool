using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace ADOMigration.Utilty;

public class WorkItemMover
{
    private string[] IterationPathsToIgnore { get; set; }
    private string[] AreaPathsToIgnore { get; set; }
    private VssConnection Connection { get; set; }
    private WorkItemTrackingHttpClient WitClient { get; set; }
    private WorkItemRelationHelper WorkItemRelationHelper { get; set; }
    private WorkItemFetcher WorkItemFetcher { get; set; }
    private ILogger Logger { get; set; }

    public HashSet<int> WorkItemMoverContext = [];

    public WorkItemMover(IConfiguration config, WorkItemFetcher workItemFetcher, WorkItemRelationHelper workItemRelationHelper, ILogger logger)
    {
        var orgUrl = new Uri(config.GetValue<string>("Migration:OrgURL") ?? string.Empty);
        var pat = config.GetValue<string>("Migration:PersonalAcessToken") ?? string.Empty;

        IterationPathsToIgnore = (config.GetValue<string>("Migration:IterationPathsToIgnore") ?? string.Empty).Split(";");
        AreaPathsToIgnore = (config.GetValue<string>("Migration:AreaPathsToIgnore") ?? string.Empty).Split(";");
        Connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, pat));
        WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
        WorkItemRelationHelper = workItemRelationHelper;
        WorkItemFetcher = workItemFetcher;
        Logger = logger;
    }

    public void MoveWorkItem(int workItemId, string sourceProject, string destinationProject)
    {
        if (WorkItemMoverContext.Contains(workItemId)) return;

        var workItem = WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

        if (workItem.Fields.Any(x => x.Key == "System.WorkItemType" && (x.Value.ToString() == "Test Case" || x.Value.ToString() == "Test Suite" || x.Value.ToString() == "Test Plan"))) return;

        WorkItemMoverContext.Add(workItemId);

        if (IterationPathsToIgnore.Contains(workItem.Fields.First(x => x.Key == "System.IterationPath").Value) || AreaPathsToIgnore.Contains(workItem.Fields.First(x => x.Key == "System.AreaPath").Value))
        {
            Logger.Log($"Skipping work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value} as AreaPath or IterationPath excluded from migration");
            return;
        }

        Logger.Log($"Started moving work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");

        MoveRelations(workItem, sourceProject, destinationProject);

        // Remove the Test Artifact Links
        var removedRelations = WorkItemRelationHelper.RemoveTestArtifactRelations(workItemId, sourceProject);

        // Move the work Item
        try
        {
            Logger.Log($"Updating TeamProject, AreaPath and IterationPath of work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");
            
            var movedWorkItem = WitClient.UpdateWorkItemAsync(GetJsonPatchToMoveWorkItem(workItem, sourceProject, destinationProject), workItemId).Result;
            
            Logger.Log($"Completed updating  work item : {movedWorkItem.Id} type: {movedWorkItem.Fields.First(x => x.Key == "System.WorkItemType").Value} with TeamProject: {movedWorkItem.Fields.First(x => x.Key == "System.TeamProject").Value}, AreaPath: {movedWorkItem.Fields.First(x => x.Key == "System.AreaPath").Value} and IterationPath: {movedWorkItem.Fields.First(x => x.Key == "System.IterationPath").Value}");

            //Add removed relations back
            if (removedRelations != null && removedRelations.Count > 0)
            {
                List<Tuple<string, string>> relationsToAdd = GetRelationsToAdd(sourceProject, destinationProject, removedRelations);

                WorkItemRelationHelper.AddRelations(workItemId, relationsToAdd);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Updating TeamProject, AreaPath and IterationPath of work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value} Failed!! Exception: {ex.Message}");
            
            return;
        }

        Logger.Log($"Completed moving work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");
    }

    private List<Tuple<string, string>> GetRelationsToAdd(string sourceProject, string destinationProject, List<Tuple<string, string>> removedRelations)
    {
        var relationsToAdd = new List<Tuple<string, string>>();
        foreach (var removedRelation in removedRelations)
        {
            var filterQuery = $"SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [System.Tags] FROM workitems WHERE [System.TeamProject] = '{destinationProject}' AND [Custom.ReflectedWorkItemId] = 'https://dev.azure.com/f16hari/{sourceProject}/_workitems/edit/{removedRelation.Item2.Split("/").Last()}'";

            var movedRelatedWorkItem = WorkItemFetcher.FetchByWITQuery(filterQuery).FirstOrDefault();
            if (movedRelatedWorkItem == null || movedRelatedWorkItem.Id == null)
            {
                Logger.Log($"Unable to find migrated work item : {removedRelation.Item2.Split("/").Last()} in the Destination Project: {destinationProject}");
                continue;
            }

            relationsToAdd.Add(new Tuple<string, string>(removedRelation.Item1, movedRelatedWorkItem.Url));
        }

        return relationsToAdd;
    }

    private void MoveRelations(WorkItem workItem, string sourceProject, string destinationProject)
    {
        if(workItem.Relations == null) return;

        Logger.Log($"Started moving the related work items of work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");

        foreach (var relation in workItem.Relations)
        {
            MoveWorkItem(int.Parse(relation.Url.Split('/').Last()), sourceProject, destinationProject);
        }

        Logger.Log($"Completed moving the related work items of work item : {workItem.Id} type: {workItem.Fields.First(x => x.Key == "System.WorkItemType").Value}");
    }

    public static JsonPatchDocument GetJsonPatchToMoveWorkItem(WorkItem workItem, string sourceProject, string destinationProject)
    {
        JsonPatchDocument jsonPatchDocument =
        [
            new JsonPatchOperation() {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.TeamProject",
                Value = destinationProject
            },
            new JsonPatchOperation() {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.AreaPath",
                Value = workItem.Fields.First(x => x.Key == "System.AreaPath").Value.ToString()?.Replace(sourceProject, destinationProject)
            },
            new JsonPatchOperation() {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.IterationPath",
                Value = workItem.Fields.First(x => x.Key == "System.IterationPath").Value.ToString()?.Replace(sourceProject, destinationProject)
            },
        ];

        return jsonPatchDocument;
    }
}
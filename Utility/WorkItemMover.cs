using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace ADOMigration.Utilty;

public class WorkItemMover(WorkItemTrackingHttpClient witClient)
{
    private ClassificationNodeHelper ClassificationNodeHelper { get; } = new ClassificationNodeHelper(witClient);

    public WorkItem MoveWorkItem(int workItemId, string sourceProject, string destinationProject, string? destAreaPath = null, string? destIterationPath = null, string? newState = null)
    {
        try
        {
            var workItem = witClient.GetWorkItemAsync(workItemId).Result;
            var destinationAreaPath = destAreaPath ?? workItem.Fields.First(x => x.Key == "System.AreaPath").Value.ToString()?.Replace(sourceProject, destinationProject) ?? string.Empty;
            var destinationIterationPath = destIterationPath ?? workItem.Fields.First(x => x.Key == "System.IterationPath").Value.ToString()?.Replace(sourceProject, destinationProject) ?? string.Empty;

            ClassificationNodeHelper.CreateOrUpdateNode(destinationProject, destinationAreaPath.Replace($"{destinationProject}\\", ""), TreeStructureGroup.Areas);
            ClassificationNodeHelper.CreateOrUpdateNode(destinationProject, destinationIterationPath.Replace($"{destinationProject}\\", ""), TreeStructureGroup.Iterations);

            JsonPatchDocument jsonPatchDocument = [
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.TeamProject",
                    Value = destinationProject
                },
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.AreaPath",
                    Value = destinationAreaPath
                },
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = destinationIterationPath
                },
            ];

            if (!newState.IsNullOrEmpty())
            {
                jsonPatchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Replace,
                    Path = "/fields/System.State",
                    Value = newState
                });
            }

            return witClient.UpdateWorkItemAsync(jsonPatchDocument, workItemId, bypassRules: true).Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while moving workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }
}
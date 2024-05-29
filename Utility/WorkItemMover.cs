using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace ADOMigration.Utilty;

public class WorkItemMover(WorkItemTrackingHttpClient witClient)
{
    public WorkItem MoveWorkItem(int workItemId, string sourceProject, string destinationProject, string? destAreaPath = null, string? destIterationPath = null)
    {
        try
        {
            var workItem = witClient.GetWorkItemAsync(workItemId).Result;

            JsonPatchDocument jsonPatchDocument = [
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.TeamProject",
                    Value = destinationProject
                },
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.AreaPath",
                    Value = destAreaPath
                            ?? workItem.Fields.First(x => x.Key == "System.AreaPath").Value
                                            .ToString()?.Replace(sourceProject, destinationProject)
                },
                new JsonPatchOperation() {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.IterationPath",
                    Value = destIterationPath
                            ?? workItem.Fields.First(x => x.Key == "System.IterationPath").Value
                                            .ToString()?.Replace(sourceProject, destinationProject)
                },
            ];

            return witClient.UpdateWorkItemAsync(jsonPatchDocument, workItemId).Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while moving workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }
}
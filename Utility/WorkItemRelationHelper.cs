using ADOMigration.Extensions;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace ADOMigration.Utilty;

public class WorkItemRelationHelper(WorkItemTrackingHttpClient witclient)
{
    public List<WorkItemRelation> RemoveTestArtifactRelations(int workItemId)
    {
        try
        {
            var patchDocument = new JsonPatchDocument();
            List<WorkItemRelation> removedRelations = [];
            var workItem = witclient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

            if(workItem.Relations == null) return removedRelations;
            
            int currentIndex = -1;
            foreach (var relation in workItem.Relations)
            {
                currentIndex++;

                WorkItem relatedWorkItem = witclient.GetWorkItemAsync(int.Parse(relation.Url.Split('/').Last())).Result;
                if (!relatedWorkItem.IsTestArtifact()) continue;

                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Remove,
                    Path = $"/relations/{currentIndex}",
                });

                removedRelations.Add(relation);
            }

            if (removedRelations.Count == 0) return removedRelations;

            var result = witclient.UpdateWorkItemAsync(patchDocument, workItemId).Result;

            return removedRelations;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while removing test artifact relations of workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }

    public void AddRelations(int workItemId, List<WorkItemRelation> relations)
    {
        if (relations.Count == 0) return;

        try
        {
            var patchDocument = new JsonPatchDocument();
            var workItem = witclient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

            foreach (var relation in relations)
            {
                patchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        relation.Rel,
                        relation.Url
                    }
                });
            }

            var result = witclient.UpdateWorkItemAsync(patchDocument, workItemId, expand: WorkItemExpand.Relations).Result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while adding relations to workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }
}
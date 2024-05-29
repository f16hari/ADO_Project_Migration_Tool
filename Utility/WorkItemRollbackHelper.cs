
using ADOMigration.Extensions;
using ADOMigration.Model;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration.Utilty;

public class RollBackHelper(WorkItemTrackingHttpClient witclient)
{
    public void RollBack(int workItemId, SystemOperation operation, string prevValue)
    {
        try
        {
            var workItem = witclient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;

            if (operation == SystemOperation.RemoveTestArtifacts)
            {
                var removedRelations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WorkItemRelation>>(prevValue)
                                       ?? throw new ArgumentNullException();

                WorkItemRelationHelper workItemRelationHelper = new(witclient);
                workItemRelationHelper.AddRelations(workItemId, removedRelations);
            }

            else if (operation == SystemOperation.MoveWorkItem)
            {
                var splits = prevValue.Split(";");
                var prevTeamProject = splits[0];
                var prevAreaPath = splits[1];
                var prevIterationPath = splits[2];

                WorkItemMover workItemMover = new(witclient);
                workItemMover.MoveWorkItem(workItemId, workItem.TeamProject(), prevTeamProject, prevAreaPath, prevIterationPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while rolling back '{operation.Description()}' operation workitem {workItemId} Exception: {ex.Message}");
            throw;
        }
    }
}
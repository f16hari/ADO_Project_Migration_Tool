using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration;

public class ClassificationNodeHelper(WorkItemTrackingHttpClient witClient)
{
    public void CreateOrUpdateNode(string project, string path, TreeStructureGroup structureGroup)
    {
        var splits = path.Split("\\");
        var nodePath = "";

        foreach (var split in splits)
        {
            try
            {
                var node = new WorkItemClassificationNode() { Name = split };
                var _ = witClient.CreateOrUpdateClassificationNodeAsync(node, project, structureGroup, nodePath).Result;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("is already in use by a different child of parent classification node")) continue;

                Console.WriteLine($"Unable to create {structureGroup} with path {path} under {project} Exception: {ex.Message}");
                throw;
            }
            finally
            {
                nodePath = nodePath.IsNullOrEmpty() ? split : $"{nodePath}\\{split}";
            }
        }
    }
}

using ADOMigration.Logging;
using ADOMigration.Model;
using ADOMigration.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using ADOMigration.Extensions;
using ADOMigration.Utilty;

namespace ADOMigration.Core;

public class Executor
{
    public ExecutorConfig Config { get; set; }
    public WorkItemTrackingHttpClient WitClient { get; }
    public ExecutionReporter Reporter { get; }
    public ExecutionLogger ExecutionLogger { get; }

    public Executor(IConfiguration config)
    {
        Config = new(config);
        ExecutionLogger = new ExecutionLogger(Config.LoggingDirectory);
        Reporter = new ExecutionReporter(Config.ReportingDirectory);


        WitClient = new VssConnection(Config.OrgURL,
                                      new VssBasicCredential(string.Empty, Config.PersonalAcessToken))
                                      .GetClient<WorkItemTrackingHttpClient>();
    }

    public void Execute(UserOperation operation)
    {
        switch (operation)
        {
            case UserOperation.GenerateReport:
                GenerateReport();
                break;
            case UserOperation.MoveWorkItems:
                MoveWorkItems();
                break;
            case UserOperation.RollBackTo:
                RollBack();
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public void GenerateReport()
    {
        WorkItemTraverser workItemTraverser = new(WitClient);
        int processedCount = 0;

        foreach (var workItemId in Config.WorkItemIds)
        {
            var workItem = WitClient.GetWorkItemAsync(workItemId).Result;
            Reporter.Add(workItem);

            if (Config.ShouldTraverseRelations)
            {
                workItemTraverser.Traverse(workItemId, new Func<int, bool>(ShouldProcess), new Action<int>(travWorkItemId =>
                {
                    var travWorkItem = WitClient.GetWorkItemAsync(travWorkItemId).Result;
                    Reporter.Add(travWorkItem);
                }));
            }

            processedCount++;
            Console.WriteLine($"Processed {processedCount} / {Config.WorkItemIds.Count}", true);
        }
    }

    public void MoveWorkItems()
    {
        WorkItemTraverser workItemTraverser = new(WitClient);
        int processedCount = 0;

        foreach (var workItemId in Config.WorkItemIds)
        {
            var workItem = WitClient.GetWorkItemAsync(workItemId).Result;

            // 1. Move WorkItem
            MoveWorkItem(workItemId);

            // 2. Move realted WorkItem
            if (Config.ShouldTraverseRelations)
            {
                workItemTraverser.Traverse(workItemId, new Func<int, bool>(ShouldProcess), new Action<int>(MoveWorkItem));
            }

            processedCount++;
            Console.WriteLine($"Processed {processedCount} / {Config.WorkItemIds.Count}", true);
        }
    }

    public void MoveWorkItem(int workItemId)
    {
        var workItem = WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations).Result;
        List<WorkItemRelation> removedRelations = [];
        WorkItemRelationHelper workItemRelationHelper = new(WitClient);
        WorkItemMover mover = new(WitClient);

        // 1. Remove Test Aritifact links
        try
        {
            removedRelations = workItemRelationHelper.RemoveTestArtifactRelations(workItemId);
            ExecutionLogger.Log(workItem,
                                SystemOperation.RemoveTestArtifacts,
                                Newtonsoft.Json.JsonConvert.SerializeObject(workItem.Relations) ?? string.Empty,
                                Newtonsoft.Json.JsonConvert.SerializeObject(removedRelations) ?? string.Empty,
                                OperationStatus.Completed);
        }
        catch
        {
            ExecutionLogger.Log(workItem,
                                SystemOperation.RemoveTestArtifacts,
                                Newtonsoft.Json.JsonConvert.SerializeObject(workItem.Relations) ?? string.Empty,
                                string.Empty,
                                OperationStatus.Failed);
            throw;
        }


        // 2. Move WorkItem
        try
        {
            var movedWorkItem = mover.MoveWorkItem(workItemId, Config.SourceProject, Config.DestinationProject);
            ExecutionLogger.Log(workItem,
                                SystemOperation.MoveWorkItem,
                                $"{workItem.TeamProject()};{workItem.AreaPath()};{workItem.IterationPath}",
                                $"{movedWorkItem.TeamProject()};{movedWorkItem.AreaPath()};{movedWorkItem.IterationPath}",
                                OperationStatus.Completed);
        }
        catch
        {
            ExecutionLogger.Log(workItem,
                                SystemOperation.MoveWorkItem,
                                $"{workItem.TeamProject()};{workItem.AreaPath()};{workItem.IterationPath}",
                                string.Empty,
                                OperationStatus.Failed);
            throw;
        }


        // 3. Restore the Test Artifact links
        try
        {
            if (removedRelations.Count == 0) return;
            workItemRelationHelper.AddRelations(workItemId, removedRelations);
            ExecutionLogger.Log(workItem,
                                SystemOperation.RestoringTestArtifactLinks,
                                Newtonsoft.Json.JsonConvert.SerializeObject(workItem.Relations) ?? string.Empty,
                                string.Empty,
                                OperationStatus.Completed);
        }
        catch
        {
            ExecutionLogger.Log(workItem,
                                SystemOperation.RestoringTestArtifactLinks,
                                Newtonsoft.Json.JsonConvert.SerializeObject(workItem.Relations) ?? string.Empty,
                                Newtonsoft.Json.JsonConvert.SerializeObject(removedRelations) ?? string.Empty,
                                OperationStatus.Failed);
            throw;
        }

    }

    public void RollBack()
    {
        try
        {
            RollBackHelper rollBackHelper = new(WitClient);

            foreach (var line in File.ReadAllLines(Config.ReportingDirectory).Reverse())
            {
                ExecutionLogEntry logEntry = new(line);
                if (logEntry.ExecutionStep < Config.RollBackToStep) break;

                rollBackHelper.RollBack(logEntry.WorkItemId, logEntry.Operation, logEntry.PrevValue);
            }

            Console.WriteLine($"Rollback till Step : {Config.RollBackToStep} completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while rolling back changes Exception: {ex.Message}");
            throw;
        }
    }

    private bool ShouldProcess(int workItemId)
    {
        var workItem = WitClient.GetWorkItemAsync(workItemId).Result;

        if (workItem.TeamProject() != Config.SourceProject
            || Config.AreaPathsToIgnore.Contains(workItem.AreaPath())
            || Config.IterationPathsToIgnore.Contains(workItem.IterationPath())
            || Config.WorkItemTypesToIgnore.Contains(workItem.WorkItemType())) return false;

        return true;
    }
}

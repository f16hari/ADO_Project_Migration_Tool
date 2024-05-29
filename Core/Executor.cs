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
    public IConfiguration Configuration { get; set; }
    public WorkItemTrackingHttpClient WitClient { get; }
    public List<int> WorkItemIds { get; }
    public bool ShouldTraverseRelations { get; }
    public string SourceProject { get; }
    public string DestinationProject { get; }
    public ExecutionReporter Reporter { get; }
    public ExecutionLogger ExecutionLogger { get; }

    public Executor(IConfiguration config)
    {
        Configuration = config;
        ExecutionLogger = new ExecutionLogger(config);
        Reporter = new ExecutionReporter(config);

        var orgUrl = new Uri(config.GetValue<string>("Migration:OrgURL") ?? string.Empty);
        var pat = config.GetValue<string>("Migration:PersonalAcessToken") ?? string.Empty;

        SourceProject = config.GetValue<string>("Migration:SourceProject") ?? string.Empty;
        DestinationProject = config.GetValue<string>("Migration:DestinationProject") ?? string.Empty;
        WorkItemIds = (config.GetValue<string>("Migration:WorkItems") ?? string.Empty).Split(",").Select(int.Parse).ToList();
        ShouldTraverseRelations = config.GetValue<bool>("Migration:ShouldTraverseRelations");

        WitClient = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, pat)).GetClient<WorkItemTrackingHttpClient>();
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

        foreach (var workItemId in WorkItemIds)
        {
            var workItem = WitClient.GetWorkItemAsync(workItemId).Result;
            Reporter.Add(workItem);

            workItemTraverser.Traverse(workItemId, new Func<int, bool>(ShouldTraverse), new Action<int>(travWorkItemId =>
            {
                var travWorkItem = WitClient.GetWorkItemAsync(travWorkItemId).Result;
                Reporter.Add(travWorkItem);
            }));
        }
    }

    public void MoveWorkItems()
    {
        foreach (var workItemId in WorkItemIds)
        {
            var workItem = WitClient.GetWorkItemAsync(workItemId).Result;

            // 1. Move WorkItem
            MoveWorkItem(workItemId);

            // 2. Move realted WorkItem
            WorkItemTraverser workItemTraverser = new(WitClient);
            workItemTraverser.Traverse(workItemId, new Func<int, bool>(ShouldTraverse), new Action<int>(MoveWorkItem));
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
            var movedWorkItem = mover.MoveWorkItem(workItemId, SourceProject, DestinationProject);
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


        // 3. Restore the links
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
            var rollBackTillStep = Configuration.GetValue<int>("Migration:RollBackTillStep");
            RollBackHelper rollBackHelper = new(WitClient);

            foreach(var line in File.ReadAllLines(Configuration.GetValue<string>("Logging:Directory")?? string.Empty).Reverse())
            {
                ExecutionLogEntry logEntry = new(line);
                if(logEntry.ExecutionStep < rollBackTillStep) break;

                rollBackHelper.RollBack(logEntry.WorkItemId, logEntry.Operation, logEntry.PrevValue);
            }

            Console.WriteLine($"Rollback till Step : {rollBackTillStep} completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while rolling back changes Exception: {ex.Message}");
            throw;
        }
    }

    public bool ShouldTraverse(int workItemId)
    {
        if (!ShouldTraverseRelations) return false;

        var workItem = WitClient.GetWorkItemAsync(workItemId).Result;
        return workItem != null && workItem.Fields.Any(x => x.Key == "System.TeamProject" && x.Value.ToString() == SourceProject);
    }
}

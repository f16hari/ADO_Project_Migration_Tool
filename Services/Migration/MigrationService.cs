using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Common;
using ADOMigration.Utilty;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace ADOMigration;

public class MigrationService(IConfiguration config, WorkItemFetcher workItemFetcher, WorkItemMover workItemMover, ILogger logger)
{
    private string WITQuery { get; set; } = config.GetValue<string>("Migration:WITQuery") ?? string.Empty;
    private string WorkItemIdsToMigrate { get; set; } = config.GetValue<string>("Migration:WorkItemIdsToMigrate") ?? string.Empty;
    private string SourceProject { get; set; } = config.GetValue<string>("Migration:SourceProject") ?? string.Empty;
    private string DestinationProject { get; set; } = config.GetValue<string>("Migration:DestinationProject") ?? string.Empty;

    public void Run()
    {
        logger.Log("Migration Started");

        List<int> workItemIdsToMove = [];

        if (WorkItemIdsToMigrate.IsNullOrEmpty())
            workItemIdsToMove = workItemFetcher.FetchByWITQuery(WITQuery).Select(x => x.Id ?? -1).Where(x => x != -1).ToList();
        else
            workItemIdsToMove = WorkItemIdsToMigrate.Split(",").Select(int.Parse).ToList();

        logger.Log($"Found {workItemIdsToMove.Count} workitems to migrate");

        int counter = 1;
        foreach (var workItemIdToMove in workItemIdsToMove)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            workItemMover.MoveWorkItem(workItemIdToMove, SourceProject, DestinationProject);

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"Processed {counter} / {workItemIdsToMove.Count}, took {elapsedMs} ms to process the record");
        }

        logger.Log("Migration Completed");
    }
}


# Things to understand before moving workitems accross the pojects

- Work items can be moved to a new team project only within the same organization.
- Work items having test artifact relations (Test Case, Test Suites, Test Plans) cannot be moved.

# Process involved in moving work items accross projects

## Prerequisites
1. Get the tool build contents.
2. Verify if it contains **ADOMigration.exe** and **appsettings.json** along with other dlls.
3. Open the cmd prompt in the same location as of the tool
4. Verify if .net 8 is installed in the environment where the tool will be run on or else install it from `[text](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)`

## Operations Supported
1. Move work items to a different team project
2. Report of all the items to be moved
3. Rollback the operations performed

## Steps for : Moving work item Operation

1. Configure the appsettings with value of **Operation** key to be **MoveWorkItems** along with other general configurations like **OrgURL** and **PersonalAccessToken** which you can get the ADO.
2. Setting the value of **ShouldTraverseRelations** to true will allow the tool to move work items specified through **WorkItemIds** property along with their related link as well.
3. **AreaPathsToIgnore, IterationPathsToIgnore, WorkItemTypesToIgnore** are array of string properties which can be used to skip work items that end up for related links but are not to be migrated. Note: If all paths under a particular area or iteration path are to be ignored add a `*` in the end e.g. A/B* which will ignore paths like A/B/C or A/B/C/D etc.
4. Having **LoggingDirectory** is mandatory as here is where the Execution history will be stored which will be later used for Rollback.
5. By default work items that are not from the **SourceProject** will not be moved.
6. Now to execute in the cmd opened just type **ADMigration** and press enter.


## Steps for : Reporting

1. Configure the appsettings with value of **Operation** key to be **GenerateReport** along with other general configurations like **OrgURL** and **PersonalAccessToken** which you can get the ADO.
2. Setting the value of **ShouldTraverseRelations** to true will allow the tool to report work items specified through **WorkItemIds** property along with their related link as well.
3. **AreaPathsToIgnore, IterationPathsToIgnore, WorkItemTypesToIgnore** are array of string properties which can be used to skip work items that end up for related links but are not to be reported.
4. Now to execute in the cmd opened just type **ADMigration** and press enter.
5. Having **ReportingDirectory** is mandatory as here is where the report with work items to be migrated will be stored.

## Steps for : Rollback

1. Configure the appsettings with value of **Operation** key to be **MoveWorkItems** along with other general configurations like **OrgURL** and **PersonalAccessToken** which you can get the ADO.
2. Configure **RollBackFile** which will be the Execution Log file generated while moving work item and **RollBackToStep** to nudge the tool to roll back up to particular step based on the execution log file.
4. Now to execute in the cmd opened just type **ADMigration** and press enter.

## Things to keep in mind
- ADO has api rate limiter so its better to perform required operation on few hundered first -> verify those -> move to the next set with some cooldown time.
- If lets say something goes wrong in the middle of moving a work item, use the Rollback operation to roll back the step done till now for that workitem and the again re run the migration. For performance work items already migrated can be removed from the **WorkItemIds**.

## appsettings.json Template
```json
{
    "OrgURL": "<Your organisation url>",
    "PersonalAcessToken": "<your acess token>",
    "SourceProject": "Source",
    "DestinationProject": "Target",
    "WorkItemIds": [],
    "WITQuery": "",
    "AreaPathsToIgnore": [],
    "IterationPathsToIgnore": [],
    "WorkItemTypesToIgnore": [],
    "ShouldTraverseRelations": false,
    "Operation": "<Can be MoveWorkItems | GenerateReport | RollBackTo>",
    "RollBackToStep": 0,
    "RollBackFile": "",
    "LoggingDirectory": "",
    "ReportingDirectory": ""
}
```

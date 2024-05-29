using System.ComponentModel;

namespace ADOMigration.Model;

public enum UserOperation
{
    MoveWorkItems,
    GenerateReport,
    RollBackTo
}

public enum SystemOperation
{
    [Description("Removing Test Artifact links")]
    RemoveTestArtifacts,
    [Description("Moving Work Item")]
    MoveWorkItem,
    [Description("Restoring Test Artifact links")]
    RestoringTestArtifactLinks
}

public enum OperationStatus
{
    Completed,
    Failed,
    RollBack
}
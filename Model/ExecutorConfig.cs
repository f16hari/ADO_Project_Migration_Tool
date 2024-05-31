using ADOMigration.Model;
using Microsoft.Extensions.Configuration;

namespace ADOMigration;

public class ExecutorConfig(IConfiguration config)
{
    public Uri OrgURL { get; set; } = new Uri(config.GetValue<string>("OrgURl") ?? throw new ArgumentNullException("OrgUrl"));
    public string PersonalAcessToken { get; set; } = config.GetValue<string>("PersonalAcessToken") ?? throw new ArgumentNullException("PersonalAcessToken");
    public string SourceProject { get; set; } = config.GetValue<string>("SourceProject") ?? throw new ArgumentNullException("SourceProject");
    public string DestinationProject { get; set; } = config.GetValue<string>("DestinationProject") ?? throw new ArgumentNullException("DestinationProject");
    public List<int> WorkItemIds { get; set; } = config.GetSection("WorkItemIds").Get<List<int>>() ?? [];
    public string WITQuery { get; set; } = config.GetValue<string>("WITQuery") ?? string.Empty;
    public Dictionary<string, string> StateMaps { get; set; } = config.GetSection("StateMaps").GetChildren().ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
    public List<string> AreaPathsToIgnore { get; set; } = config.GetSection("AreaPathsToIgnore").Get<List<string>>() ?? [];
    public List<string> IterationPathsToIgnore { get; set; } = config.GetSection("IterationPathsToIgnore").Get<List<string>>() ?? [];
    public List<string> WorkItemTypesToIgnore { get; set; } = config.GetSection("WorkItemTypesToIgnore").Get<List<string>>() ?? [];
    public bool ShouldTraverseRelations { get; set; } = config.GetValue<bool>("ShouldTraverseRelations");
    public UserOperation Operation { get; set; } = (UserOperation)Enum.Parse(typeof(UserOperation), config.GetValue<string>("Operation") ?? throw new ArgumentNullException("Operation"));
    public long? RollBackToStep { get; set; } = config.GetValue<long>("RollBackToStep");
    public string RollBackFile { get; set; } = config.GetValue<string>("RollBackFile") ?? string.Empty;
    public string ReportingDirectory { get; set; } = config.GetValue<string>("ReportingDirectory") ?? throw new ArgumentNullException("ReportingDirectory");
    public string LoggingDirectory { get; set; } = config.GetValue<string>("LoggingDirectory") ?? throw new ArgumentNullException("LoggingDirectory");
}

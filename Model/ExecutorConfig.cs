using ADOMigration.Model;
using Microsoft.Extensions.Configuration;

namespace ADOMigration;

public class ExecutorConfig(IConfiguration config)
{
    public Uri OrgURL { get; set; } = new Uri(config.GetValue<string>("OrgURl") ?? throw new ArgumentNullException("OrgUrl"));
    public string PersonalAcessToken { get; set; } = config.GetValue<string>("PersonalAcessToken") ?? throw new ArgumentNullException("PersonalAcessToken");
    public string SourceProject { get; set; } = config.GetValue<string>("SourceProject") ?? throw new ArgumentNullException("SourceProject");
    public string DestinationProject { get; set; } = config.GetValue<string>("DestinationProject") ?? throw new ArgumentNullException("DestinationProject");
    public List<int> WorkItemIds { get; set; } = config.GetValue<List<int>>("WorkItemIds") ?? [];
    public string WITQuery { get; set; } = config.GetValue<string>("WITQuery") ?? string.Empty;
    public List<string> AreaPathsToIgnore { get; set; } = config.GetValue<List<string>>("AreaPathsToIgnore") ?? [];
    public List<string> IterationPathsToIgnore { get; set; } = config.GetValue<List<string>>("IterationPathsToIgnore") ?? [];
    public List<string> WorkItemTypesToIgnore { get; set; } = config.GetValue<List<string>>("WorkItemTypesToIgnore") ?? [];
    public bool ShouldTraverseRelations { get; set; } = config.GetValue<bool>("ShouldTraverseRelations");
    public UserOperation Operation { get; set; } = (UserOperation)Enum.Parse(typeof(UserOperation), config.GetValue<string>("Operation") ?? throw new ArgumentNullException("Operation"));
    public long? RollBackToStep { get; set; } = config.GetValue<long>("RollBackToStep");
    public string ReportingDirectory { get; set; } = config.GetValue<string>("ReportingDirectory") ?? throw new ArgumentNullException("ReportingDirectory");
    public string LoggingDirectory { get; set; } = config.GetValue<string>("LoggingDirectory") ?? throw new ArgumentNullException("LoggingDirectory");
}

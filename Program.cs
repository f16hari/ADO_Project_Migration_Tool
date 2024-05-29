using Microsoft.Extensions.Configuration;
using ADOMigration.Model;
using ADOMigration.Core;

var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json");

var configuration = builder.Build();

UserOperation operationToPerform = (UserOperation)Enum.Parse(typeof(UserOperation), configuration.GetValue<string>("Migration:OperationToPerform") ?? string.Empty);
Executor executor = new(configuration);

switch (operationToPerform)
{
        case UserOperation.MoveWorkItems:
                executor.MoveWorkItems();
                break;
        case UserOperation.RollBackTo:
                executor.RollBack();
                break;
        case UserOperation.GenerateReport:
                executor.GenerateReport();
                break;
        default:
                throw new InvalidOperationException();
}
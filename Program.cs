using Microsoft.Extensions.Configuration;
using ADOMigration;
using Microsoft.Extensions.DependencyInjection;
using ADOMigration.Utilty;

var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json");

var configuration = builder.Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(provider => configuration);
services.AddSingleton<WorkItemFetcher, WorkItemFetcher>();
services.AddSingleton<WorkItemMover, WorkItemMover>();
services.AddSingleton<WorkItemRelationHelper, WorkItemRelationHelper>();
services.AddSingleton<ILogger>(provider => new FileLogger(configuration.GetValue<string>("Logging:FilePath") ?? string.Empty));
services.AddSingleton<MigrationService, MigrationService>();

services.BuildServiceProvider().GetService<MigrationService>()?.Run();
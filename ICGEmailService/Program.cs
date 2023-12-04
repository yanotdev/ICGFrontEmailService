using ICGEmailService;
using ICGEmailService.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IDataService, DataService>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

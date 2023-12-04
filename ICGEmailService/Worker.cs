using ICGEmailService.Service;

namespace ICGEmailService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDataService _dataService;

        public Worker(ILogger<Worker> logger, IDataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool checkDb = await _dataService.CheckTableAsync("NewSaleToSend");
            bool checkTriger = await _dataService.CheckTriggerExistsAsync("afterSales_Insert");
            if (!checkDb)
            {
                _logger.LogInformation("{0}db not found", checkDb);
            }
            if (!checkTriger)
            {
                _logger.LogInformation("{0} trigger not found", checkTriger);
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                await _dataService.ProcessPendingEmails();
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
//, _configuration.GetConnectionString("Default")
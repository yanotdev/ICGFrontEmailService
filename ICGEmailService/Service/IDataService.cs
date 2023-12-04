using Microsoft.Data.SqlClient;

namespace ICGEmailService.Service
{
    public interface IDataService
    {
        Task<bool> CheckTableAsync(string tableName);
        Task<bool> CreateTriger();
        Task ProcessPendingEmails();
        Task<bool> CheckTriggerExistsAsync(string triggerName);
    }
}
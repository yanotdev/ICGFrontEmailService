using ICGEmailService.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ICGEmailService.Service
{
    public class DataService : IDataService
    {
        private static IConfiguration? _configuration;
        private static string? _connectionString;
        private readonly ILogger<Worker> _logger;
        public DataService(IConfiguration configuration, ILogger<Worker> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("Default");
            _logger = logger;
        }
        public async Task ProcessPendingEmails()
        {
            try
            {
                List<NewSales> newSalesList = await GetAllNewSales();

                if (newSalesList.Count > 0)
                {
                    var tasks = newSalesList.Select(async sale =>
                    {
                        var (clientName, clientEmail) = await GetClientInfoById(sale.CustomerId);

                        if (!string.IsNullOrEmpty(clientName) && !string.IsNullOrEmpty(clientEmail))
                        {
                            string subject = $"Your Recent Purchase - Invoice: {sale.InvoiceNumber}, Serial: {sale.Serie}";
                            string body = $"Dear {clientName},<br><br>" +
                                          $"Thank you for your recent purchase!<br><br>" +
                                          $"Transaction Details:<br>" +
                                          $"Invoice Number: {sale.InvoiceNumber}<br>" +
                                          $"Serial Number: {sale.Serie}<br>" +
                                          $"Total Cost: {sale.Cost}<br><br>" +
                                          $"Please let us know if you have any questions.<br><br>" +
                                          $"Best regards,<br>Your Company";

                            // Send email asynchronously
                            bool sent = await Sender.SendMail(clientEmail, subject, body);

                            if (sent)
                            {
                                // Delete processed pending email
                                await DeleteProcessedPendingEmail(sale.InvoiceNumber, sale.Serie);
                            }
                        }
                        // Delete processed pending email
                        await DeleteProcessedPendingEmail(sale.InvoiceNumber, sale.Serie);
                    }).ToList();

                    // Await all tasks to complete
                    await Task.WhenAll(tasks);
                }
                else
                {
                    Console.WriteLine("No pending sales to send found.");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing pending emails: " + ex.Message);
            }
        }

        public async Task<bool> CreateTriger()
        {
            string triggerQuery = "";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(triggerQuery, connection);
                try
                {
                    await connection.OpenAsync();
                    command.ExecuteNonQuery();
                    Console.WriteLine("Trigger created successfully!");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error creating trigger: " + ex.Message);
                    return false;
                }
            }
        }

        public async Task<bool> CheckTableAsync(string tableName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    bool tableExists = await CheckIfTableExistsAsync(connection, tableName);

                    if (tableExists)
                    {
                        //_logger.LogInformation($"Table '{tableName}' exists in the database.");
                        return true;
                    }
                    else
                    {
                        //_logger.LogInformation($"Table '{tableName}' does not exist in the database.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> CheckIfTableExistsAsync(SqlConnection connection, string tableName)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
                command.Parameters.AddWithValue("@TableName", tableName);

                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }

        public async Task<bool> CheckTriggerExistsAsync(string triggerName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    bool triggerExists = await CheckIfTriggerExistsAsync(connection, triggerName);

                    if (triggerExists)
                    {
                        //_logger.LogInformation($"Trigger '{triggerName}' exists in the database.");
                        return true;
                    }
                    else
                    {
                        //_logger.LogInformation($"Trigger '{triggerName}' does not exist in the database.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> CheckIfTriggerExistsAsync(SqlConnection connection, string triggerName)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "SELECT COUNT(*) FROM sys.triggers WHERE name = @TriggerName";
                command.Parameters.AddWithValue("@TriggerName", triggerName);

                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
        }

        public async Task<List<NewSales>> GetAllNewSales()
        {
            List<NewSales> newSalesList = new List<NewSales>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM dbo.NewSaleToSend";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                NewSales newSale = new NewSales
                                {
                                    InvoiceNumber = reader.GetInt32(reader.GetOrdinal("invoice_number")),
                                    CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                                    Serie = reader.GetString(reader.GetOrdinal("serie")),
                                    Cost = reader.GetDecimal(reader.GetOrdinal("cost"))
                                };
                                _logger.LogInformation($"cost: {newSale.Cost} \n customerID: {newSale.CustomerId}");
                                newSalesList.Add(newSale);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Error fetching new sales: " + ex.Message);
            }

            return newSalesList;
        }

        public static async Task<(string Name, string Email)> GetClientInfoById(int clientId)
        {
            string clientName = string.Empty;
            string clientEmail = string.Empty;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT NOMBRECLIENTE, E_MAIL FROM CLIENTES WHERE CODCLIENTE = @clientId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                clientName = reader.GetString(reader.GetOrdinal("NOMBRECLIENTE"));
                                clientEmail = reader.GetString(reader.GetOrdinal("E_MAIL"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching client info: " + ex.Message);
            }

            return (clientName, clientEmail);
        }

        private async Task DeleteProcessedPendingEmail(int invoiceNumber, string serie)
        {
            SqlTransaction transaction = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Start a transaction
                    transaction = connection.BeginTransaction();

                    // Assuming 'NewSaleToSend' is the table name where the pending emails are stored
                    string deleteQuery = "DELETE FROM NewSaleToSend WHERE serie = @serie AND invoice_number = @invoice_number;";

                    using (SqlCommand command = new SqlCommand(deleteQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@serie", serie);
                        command.Parameters.AddWithValue("@invoice_number", invoiceNumber);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            _logger.LogInformation($"Pending email with ID {serie} {invoiceNumber} deleted successfully!");
                        }
                        else
                        {
                            _logger.LogInformation($"No pending email found with ID {serie} {invoiceNumber}.");
                        }
                    }

                    // Commit the transaction if all commands succeed
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // Roll back the transaction if any error occurs
                Console.WriteLine("Error deleting pending email: " + ex.Message);
                transaction?.Rollback();
            }
        }




    }
}
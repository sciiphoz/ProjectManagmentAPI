using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ProjectManagementAPI.Services
{
    public abstract class BaseService
    {
        protected readonly ILogger _logger;

        protected BaseService(ILogger logger)
        {
            _logger = logger;
        }

        protected async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action, string errorMessage)
        {
            try
            {
                return await action();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL ошибка: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Ошибка базы данных. {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка обновления БД: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Ошибка сохранения данных. {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Операция отменена: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Операция прервана.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: {ex.Message}");
            }
        }

        protected async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string errorMessage)
        {
            try
            {
                await action();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL ошибка: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Ошибка базы данных. {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка обновления БД: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Ошибка сохранения данных. {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Операция отменена: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: Операция прервана.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка: {Message}", ex.Message);
                throw new Exception($"{errorMessage}: {ex.Message}");
            }
        }
    }
}
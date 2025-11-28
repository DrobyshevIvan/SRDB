using Domain.Entities;

namespace Infrastructure.Services;

public interface IStoredProcedureService
{
    Task<int> PurchaseProductAsync(int productId, int userId, int quantity = 1, int? orderId = null);
}


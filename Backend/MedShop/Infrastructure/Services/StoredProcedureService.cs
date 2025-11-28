using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StoredProcedureService : IStoredProcedureService
{
    private readonly MedShopDbContext _context;

    public StoredProcedureService(MedShopDbContext context)
    {
        _context = context;
    }

    public async Task<int> PurchaseProductAsync(int productId, int userId, int quantity = 1, int? orderId = null)
    {
        var productIdParam = new SqlParameter("@ProductId", productId);
        var userIdParam = new SqlParameter("@UserId", userId);
        var quantityParam = new SqlParameter("@Quantity", quantity);
        var orderIdParam = orderId.HasValue
            ? new SqlParameter("@OrderId", orderId.Value)
            : new SqlParameter("@OrderId", DBNull.Value);

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC usp_PurchaseProduct @ProductId, @UserId, @Quantity, @OrderId",
                productIdParam, userIdParam, quantityParam, orderIdParam);
            
            return 1;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Помилка при виконанні покупки: {ex.Message}", ex);
        }
    }

}


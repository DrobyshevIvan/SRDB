using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class FunctionService : IFunctionService
{
    private readonly MedShopDbContext _context;

    public FunctionService(MedShopDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserWithExpensiveProduct>> GetUsersWithExpensiveProductsInCategoryAsync(
        decimal minPrice, int categoryId)
    {
        var minPriceParam = new SqlParameter("@min_price", minPrice);
        var categoryIdParam = new SqlParameter("@category_id", categoryId);

        var result = await _context.Database
            .SqlQueryRaw<UserWithExpensiveProduct>(
                "SELECT * FROM GetUsersWithExpensiveProductsInCategory(@min_price, @category_id)",
                minPriceParam, categoryIdParam)
            .ToListAsync();

        return result;
    }

    public async Task<int> CountOrdersAsync(decimal maxAmount)
    {
        var maxAmountParam = new SqlParameter("@money", maxAmount);
        
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT dbo.COUNT_ORDERS(@money)";
            command.Parameters.Add(new SqlParameter("@money", maxAmount));
            var scalarResult = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalarResult ?? 0);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}


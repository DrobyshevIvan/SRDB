namespace Infrastructure.Services;

public interface IFunctionService
{
    Task<IEnumerable<UserWithExpensiveProduct>> GetUsersWithExpensiveProductsInCategoryAsync(decimal minPrice, int categoryId);
    Task<int> CountOrdersAsync(decimal maxAmount);
}

public class UserWithExpensiveProduct
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
}


using API.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MedShopDbContext _context;
    private readonly IStoredProcedureService _storedProcedureService;
    private readonly IFunctionService _functionService;

    public ProductsController(
        MedShopDbContext context,
        IStoredProcedureService storedProcedureService,
        IFunctionService functionService)
    {
        _context = context;
        _storedProcedureService = storedProcedureService;
        _functionService = functionService;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .ToListAsync();

        var productsDto = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Quantity = p.Quantity,
            SKU = p.SKU,
            ImageUrl = p.ImageUrl,
            Category = new CategoryDto
            {
                Id = p.Category.Id,
                Name = p.Category.Name,
                Description = p.Category.Description
            }
        }).ToList();

        return Ok(productsDto);
    }

    // GET: api/products/{id} - детальна інформація про товар з усіма продажами
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.OrderItems)
                .ThenInclude(oi => oi.Order)
                    .ThenInclude(o => o.User)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound($"Товар з ID {id} не знайдено");
        }

        var productDto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Quantity = product.Quantity,
            SKU = product.SKU,
            ImageUrl = product.ImageUrl,
            Category = new CategoryDto
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                Description = product.Category.Description
            },
            OrderItems = product.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                Order = new OrderSummaryDto
                {
                    Id = oi.Order.Id,
                    OrderDate = oi.Order.OrderDate,
                    TotalAmount = oi.Order.TotalAmount,
                    Status = oi.Order.Status,
                    User = new UserSummaryDto
                    {
                        Id = oi.Order.User.Id,
                        UserName = oi.Order.User.UserName,
                        FullName = oi.Order.User.FullName
                    }
                }
            }).ToList()
        };

        return Ok(productDto);
    }

    // 2. Виклик збережених процедур
    // POST: api/products/purchase
    [HttpPost("purchase")]
    public async Task<IActionResult> PurchaseProduct([FromBody] PurchaseProductDto dto)
    {
        try
        {
            await _storedProcedureService.PurchaseProductAsync(
                dto.ProductId,
                dto.UserId,
                dto.Quantity,
                dto.OrderId);

            return Ok(new { message = "Покупка успішно виконана" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (SqlException ex)
        {
            if (ex.Number >= 50000 || ex.Number == 0)
            {
                return BadRequest(new
                {
                    error = "Помилка виконання операції",
                    message = ex.Message,
                    errorNumber = ex.Number,
                    severity = ex.Class,
                    source = "Stored Procedure"
                });
            }

            return StatusCode(500, new
            {
                error = "Помилка бази даних",
                message = ex.Message,
                errorNumber = ex.Number,
                severity = ex.Class
            });
        }
    }

    // 3. Виконання функцій
    // GET: api/products/users-with-expensive-products?minPrice={price}&categoryId={id}
    [HttpGet("users-with-expensive-products")]
    public async Task<ActionResult> GetUsersWithExpensiveProducts(
        [FromQuery] decimal minPrice,
        [FromQuery] int categoryId)
    {
        try
        {
            var users = await _functionService.GetUsersWithExpensiveProductsInCategoryAsync(minPrice, categoryId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // GET: api/products/count-orders?maxAmount={amount}
    [HttpGet("count-orders")]
    public async Task<ActionResult<int>> CountOrders([FromQuery] decimal maxAmount)
    {
        try
        {
            var count = await _functionService.CountOrdersAsync(maxAmount);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}


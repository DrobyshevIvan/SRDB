using API.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MedShopDbContext _context;

    public OrdersController(MedShopDbContext context)
    {
        _context = context;
    }

    // GET: api/orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
            .ToListAsync();

        var ordersDto = orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderDate = o.OrderDate,
            TotalAmount = o.OrderItems.Sum(oi => oi.TotalPrice),
            Status = o.Status,
            User = new UserSummaryDto
            {
                Id = o.User.Id,
                UserName = o.User.UserName,
                FullName = o.User.FullName
            },
            OrderItems = o.OrderItems.Select(oi => new OrderItemDetailDto
            {
                Id = oi.Id,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                Product = new ProductSummaryDto
                {
                    Id = oi.Product.Id,
                    Name = oi.Product.Name,
                    Price = oi.Product.Price,
                    Category = oi.Product.Category != null ? new CategoryDto
                    {
                        Id = oi.Product.Category.Id,
                        Name = oi.Product.Category.Name,
                        Description = oi.Product.Category.Description
                    } : null
                }
            }).ToList()
        }).ToList();

        return Ok(ordersDto);
    }

    // GET: api/orders/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound($"Замовлення з ID {id} не знайдено");
        }

        var orderDto = new OrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice),
            Status = order.Status,
            User = new UserSummaryDto
            {
                Id = order.User.Id,
                UserName = order.User.UserName,
                FullName = order.User.FullName
            },
            OrderItems = order.OrderItems.Select(oi => new OrderItemDetailDto
            {
                Id = oi.Id,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                TotalPrice = oi.TotalPrice,
                Product = new ProductSummaryDto
                {
                    Id = oi.Product.Id,
                    Name = oi.Product.Name,
                    Price = oi.Product.Price,
                    Category = oi.Product.Category != null ? new CategoryDto
                    {
                        Id = oi.Product.Category.Id,
                        Name = oi.Product.Category.Name,
                        Description = oi.Product.Category.Description
                    } : null
                }
            }).ToList()
        };

        return Ok(orderDto);
    }

    // POST: api/orders
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var newOrderId = await InsertOrderDirectlyAsync(dto);

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstAsync(o => o.Id == newOrderId);

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                TotalAmount = order.OrderItems.Sum(oi => oi.TotalPrice),
                Status = order.Status,
                User = new UserSummaryDto
                {
                    Id = order.User.Id,
                    UserName = order.User.UserName,
                    FullName = order.User.FullName
                },
                OrderItems = order.OrderItems.Select(oi => new OrderItemDetailDto
                {
                    Id = oi.Id,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice,
                    Product = new ProductSummaryDto
                    {
                        Id = oi.Product.Id,
                        Name = oi.Product.Name,
                        Price = oi.Product.Price,
                        Category = oi.Product.Category != null ? new CategoryDto
                        {
                            Id = oi.Product.Category.Id,
                            Name = oi.Product.Category.Name,
                            Description = oi.Product.Category.Description
                        } : null
                    }
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, orderDto);
        }
        catch (SqlException sqlEx)
        {
            if (sqlEx.Number >= 50000 || sqlEx.Number == 0)
            {
                return BadRequest(new
                {
                    error = "Помилка створення замовлення",
                    message = sqlEx.Message,
                    errorNumber = sqlEx.Number,
                    severity = sqlEx.Class,
                    source = "Database Trigger",
                    details = "Це виключення було згенеровано тригером на стороні сервера БД через RAISERROR"
                });
            }

            return StatusCode(500, new
            {
                error = "Помилка бази даних",
                message = sqlEx.Message,
                errorNumber = sqlEx.Number,
                severity = sqlEx.Class
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<int> InsertOrderDirectlyAsync(CreateOrderDto dto)
    {
        var connectionString = _context.Database.GetConnectionString();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Orders(UserId, OrderDate, TotalAmount, Status)
            VALUES (@UserId, @OrderDate, 0, 'Pending');
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        command.Parameters.Add(new SqlParameter("@UserId", dto.UserId));
        command.Parameters.Add(new SqlParameter("@OrderDate", dto.OrderDate ?? DateTime.Today));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}

public class CreateOrderDto
{
    public int UserId { get; set; }
    public DateTime? OrderDate { get; set; }
}


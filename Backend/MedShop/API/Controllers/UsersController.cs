using API.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MedShopDbContext _context;

    public UsersController(MedShopDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.Orders)
            .ToListAsync();

        var usersDto = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            FullName = u.FullName,
            Email = u.Email,
            Orders = u.Orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                User = new UserSummaryDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    FullName = u.FullName
                }
            }).ToList()
        }).ToList();

        return Ok(usersDto);
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound($"Користувач з ID {id} не знайдено");
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Orders = user.Orders.Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                User = new UserSummaryDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    FullName = user.FullName
                }
            }).ToList()
        };

        return Ok(userDto);
    }
}


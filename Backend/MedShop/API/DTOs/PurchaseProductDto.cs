namespace API.DTOs;

public class PurchaseProductDto
{
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? OrderId { get; set; }
}


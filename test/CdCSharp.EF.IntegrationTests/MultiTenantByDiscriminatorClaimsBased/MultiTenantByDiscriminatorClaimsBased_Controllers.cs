using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.MultiTenantByDiscriminatorClaimsBased;

[ApiController]
[Route("api/discriminator-claims/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorClaimsBased_DbContext _context;

    public ProductsController(MultiTenantByDiscriminatorClaimsBased_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        List<MultiTenantByDiscriminatorClaimsBased_Product> products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        MultiTenantByDiscriminatorClaimsBased_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] MultiTenantByDiscriminatorClaimsBased_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorClaimsBased_Product product = new()
        {
            Name = request.Name,
            Price = request.Price,
            Category = request.Category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] MultiTenantByDiscriminatorClaimsBased_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorClaimsBased_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        product.Name = request.Name;
        product.Price = request.Price;
        product.Category = request.Category;

        await _context.SaveChangesAsync();
        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        MultiTenantByDiscriminatorClaimsBased_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

[ApiController]
[Route("api/discriminator-claims/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorClaimsBased_DbContext _context;

    public OrdersController(MultiTenantByDiscriminatorClaimsBased_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        List<MultiTenantByDiscriminatorClaimsBased_Order> orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        MultiTenantByDiscriminatorClaimsBased_Order? order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] MultiTenantByDiscriminatorClaimsBased_CreateOrderRequest request)
    {
        MultiTenantByDiscriminatorClaimsBased_Order order = new()
        {
            CustomerName = request.CustomerName,
            OrderDate = DateTime.UtcNow
        };

        foreach (MultiTenantByDiscriminatorClaimsBased_CreateOrderItemRequest itemRequest in request.Items)
        {
            MultiTenantByDiscriminatorClaimsBased_Product? product = await _context.Products.FindAsync(itemRequest.ProductId);
            if (product == null)
                return BadRequest($"Product with ID {itemRequest.ProductId} not found");

            MultiTenantByDiscriminatorClaimsBased_OrderItem orderItem = new()
            {
                ProductId = product.Id,
                Product = product,
                Quantity = itemRequest.Quantity,
                UnitPrice = product.Price
            };

            order.Items.Add(orderItem);
        }

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}

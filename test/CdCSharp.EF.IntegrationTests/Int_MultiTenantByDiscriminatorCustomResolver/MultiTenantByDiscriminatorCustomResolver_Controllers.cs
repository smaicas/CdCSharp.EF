using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorCustomResolver;

[ApiController]
[Route("api/discriminator-resolver/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorCustomResolver_DbContext _context;

    public ProductsController(MultiTenantByDiscriminatorCustomResolver_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        List<MultiTenantByDiscriminatorCustomResolver_Product> products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        MultiTenantByDiscriminatorCustomResolver_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] MultiTenantByDiscriminatorCustomResolver_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorCustomResolver_Product product = new()
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
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] MultiTenantByDiscriminatorCustomResolver_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorCustomResolver_Product? product = await _context.Products.FindAsync(id);
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
        MultiTenantByDiscriminatorCustomResolver_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

[ApiController]
[Route("api/discriminator-resolver/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorCustomResolver_DbContext _context;

    public OrdersController(MultiTenantByDiscriminatorCustomResolver_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        List<MultiTenantByDiscriminatorCustomResolver_Order> orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        MultiTenantByDiscriminatorCustomResolver_Order? order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] MultiTenantByDiscriminatorCustomResolver_CreateOrderRequest request)
    {
        MultiTenantByDiscriminatorCustomResolver_Order order = new()
        {
            CustomerName = request.CustomerName,
            OrderDate = DateTime.UtcNow
        };

        foreach (MultiTenantByDiscriminatorCustomResolver_CreateOrderItemRequest itemRequest in request.Items)
        {
            MultiTenantByDiscriminatorCustomResolver_Product? product = await _context.Products.FindAsync(itemRequest.ProductId);
            if (product == null)
                return BadRequest($"Product with ID {itemRequest.ProductId} not found");

            MultiTenantByDiscriminatorCustomResolver_OrderItem orderItem = new()
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

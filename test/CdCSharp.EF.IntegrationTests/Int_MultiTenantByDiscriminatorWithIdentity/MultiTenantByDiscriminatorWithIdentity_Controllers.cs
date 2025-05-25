using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDiscriminatorWithIdentity;

[ApiController]
[Route("api/discriminator-identity/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorWithIdentity_DbContext _context;

    public ProductsController(MultiTenantByDiscriminatorWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        List<MultiTenantByDiscriminatorWithIdentity_Product> products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        MultiTenantByDiscriminatorWithIdentity_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] MultiTenantByDiscriminatorWithIdentity_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorWithIdentity_Product product = new()
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
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] MultiTenantByDiscriminatorWithIdentity_CreateProductRequest request)
    {
        MultiTenantByDiscriminatorWithIdentity_Product? product = await _context.Products.FindAsync(id);
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
        MultiTenantByDiscriminatorWithIdentity_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

[ApiController]
[Route("api/discriminator-identity/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorWithIdentity_DbContext _context;

    public OrdersController(MultiTenantByDiscriminatorWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        List<MultiTenantByDiscriminatorWithIdentity_Order> orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        MultiTenantByDiscriminatorWithIdentity_Order? order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] MultiTenantByDiscriminatorWithIdentity_CreateOrderRequest request)
    {
        MultiTenantByDiscriminatorWithIdentity_Order order = new()
        {
            CustomerName = request.CustomerName,
            OrderDate = DateTime.UtcNow
        };

        foreach (MultiTenantByDiscriminatorWithIdentity_CreateOrderItemRequest itemRequest in request.Items)
        {
            MultiTenantByDiscriminatorWithIdentity_Product? product = await _context.Products.FindAsync(itemRequest.ProductId);
            if (product == null)
                return BadRequest($"Product with ID {itemRequest.ProductId} not found");

            MultiTenantByDiscriminatorWithIdentity_OrderItem orderItem = new()
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

[ApiController]
[Route("api/discriminator-identity/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorWithIdentity_DbContext _context;

    public UsersController(MultiTenantByDiscriminatorWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        List<IdentityUser<Guid>> users = await _context.Users.ToListAsync();
        return Ok(users.Select(u => new { u.Id, u.UserName, u.Email }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        IdentityUser<Guid>? user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        return Ok(new { user.Id, user.UserName, user.Email });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] MultiTenantByDiscriminatorWithIdentity_CreateUserRequest request)
    {
        IdentityUser<Guid> user = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            NormalizedUserName = request.UserName.ToUpper(),
            NormalizedEmail = request.Email.ToUpper(),
            EmailConfirmed = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.UserName, user.Email });
    }
}

[ApiController]
[Route("api/discriminator-identity/[controller]")]
public class RolesController : ControllerBase
{
    private readonly MultiTenantByDiscriminatorWithIdentity_DbContext _context;

    public RolesController(MultiTenantByDiscriminatorWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        List<IdentityRole<Guid>> roles = await _context.Roles.ToListAsync();
        return Ok(roles.Select(r => new { r.Id, r.Name }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
        IdentityRole<Guid>? role = await _context.Roles.FindAsync(id);
        if (role == null)
            return NotFound();

        return Ok(new { role.Id, role.Name });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] MultiTenantByDiscriminatorWithIdentity_CreateRoleRequest request)
    {
        IdentityRole<Guid> role = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            NormalizedName = request.Name.ToUpper()
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new { role.Id, role.Name });
    }
}

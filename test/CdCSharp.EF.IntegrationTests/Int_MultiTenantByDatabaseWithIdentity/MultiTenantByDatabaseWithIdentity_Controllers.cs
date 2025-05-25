using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_MultiTenantByDatabaseWithIdentity;

[ApiController]
[Route("api/database-identity/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly MultiTenantByDatabaseWithIdentity_DbContext _context;

    public ProductsController(MultiTenantByDatabaseWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        List<MultiTenantByDatabaseWithIdentity_Product> products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] MultiTenantByDatabaseWithIdentity_CreateProductRequest request)
    {
        MultiTenantByDatabaseWithIdentity_Product product = new()
        {
            Name = request.Name,
            Price = request.Price,
            Category = request.Category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
    }
}

[ApiController]
[Route("api/database-identity/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MultiTenantByDatabaseWithIdentity_DbContext _context;

    public UsersController(MultiTenantByDatabaseWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        List<IdentityUser<Guid>> users = await _context.Users.ToListAsync();
        return Ok(users.Select(u => new { u.Id, u.UserName, u.Email }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] MultiTenantByDatabaseWithIdentity_CreateUserRequest request)
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

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, new { user.Id, user.UserName, user.Email });
    }
}

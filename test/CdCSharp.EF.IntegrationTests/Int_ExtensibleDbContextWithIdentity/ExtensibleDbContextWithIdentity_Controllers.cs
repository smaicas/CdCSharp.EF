using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CdCSharp.EF.IntegrationTests.Int_ExtensibleDbContextWithIdentity;

[ApiController]
[Route("api/extensible-identity/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ExtensibleDbContextWithIdentity_DbContext _context;

    public ProductsController(ExtensibleDbContextWithIdentity_DbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        List<ExtensibleDbContextWithIdentity_Product> products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        ExtensibleDbContextWithIdentity_Product? product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ExtensibleDbContextWithIdentity_CreateProductRequest request)
    {
        ExtensibleDbContextWithIdentity_Product product = new()
        {
            Name = request.Name,
            Price = request.Price,
            Category = request.Category
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}

[ApiController]
[Route("api/extensible-identity/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ExtensibleDbContextWithIdentity_DbContext _context;

    public UsersController(ExtensibleDbContextWithIdentity_DbContext context) => _context = context;

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
    public async Task<IActionResult> CreateUser([FromBody] ExtensibleDbContextWithIdentity_CreateUserRequest request)
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
[Route("api/extensible-identity/[controller]")]
public class RolesController : ControllerBase
{
    private readonly ExtensibleDbContextWithIdentity_DbContext _context;

    public RolesController(ExtensibleDbContextWithIdentity_DbContext context) => _context = context;

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
    public async Task<IActionResult> CreateRole([FromBody] ExtensibleDbContextWithIdentity_CreateRoleRequest request)
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

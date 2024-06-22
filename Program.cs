using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Loja.data;
using Loja.services;
using Loja.models;
using Microsoft.OpenApi.Models;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Configure a conexão com o BD
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LojaDbContext>(options => 
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// Configuração da autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("abc"))
    };
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<VendaService>();

// Adicionar serviços do Swagger ao contêiner
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja API", Version = "v1" });
});

// Adicionar serviços de autorização
builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware para roteamento
app.UseRouting();

// Middleware para autenticação
app.UseAuthentication();

// Middleware para autorização
app.UseAuthorization();

// Definição das rotas
app.MapGet("/rotaProtegida", async (HttpContext context) =>
{
    // Verifica se o token está presente no cabeçalho de autorização
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Token não fornecido");
        return;
    }

    // Obtém o token do cabeçalho de autorização
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

    // Valida o token
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes("abc");
    var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    }, out var validatedToken);

    // Retorna o nome de usuário (email) presente no token
    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
    await context.Response.WriteAsync($"Usuário autenticado: {email}");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Loja API v1");
    });
}

app.UseHttpsRedirection();

// Método para gerar o token (deve ser movido para uma classe separada posteriormente)
string GenerateToken(string email)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes("senhasegura123");
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("email", email) }),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

// Endpoint de Login
app.MapPost("/login", async (HttpContext context) =>
{
    // Receber o request
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    // Deserializar o objeto
    var json = JsonDocument.Parse(body);
    var username = json.RootElement.GetProperty("username").GetString();
    var email = json.RootElement.GetProperty("email").GetString();
    var senha = json.RootElement.GetProperty("senha").GetString();

    // Esta parte do código será complementada com a service na próxima aula
    var token = "";
    if (senha == "1029") // Exemplo de validação de senha
    {
        token = GenerateToken(email);
    }
    
    await context.Response.WriteAsync(token);
});

// Rota Segura
app.MapGet("/rotaSegura", async (HttpContext context) =>
{
    // Verificar se o token está presente
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Token não fornecido");
        return;
    }

    // Obter o token
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

    // Validar o token
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes("abcabcabcabcabcabcabcabcabcabcabc"); // Chave secreta (a mesma utilizada para gerar o token)
    try
    {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
        }, out SecurityToken validatedToken);
    }
    catch
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Token inválido");
        return;
    }

    await context.Response.WriteAsync("Acesso autorizado");
});


//---------------------------------------------------------------------


//<<<----------Produtos----------->>>

// Método para gravar um novo produto
app.MapPost("/createproduto", async (Produto produto, ProductService productService) =>
{
    await productService.AddProductAsync(produto);
    return Results.Created($"/produtos/{produto.Id}", produto);
});

// Método para consultar todos os produtos
app.MapGet("/produtos", async (ProductService productService) =>
{
    var produtos = await productService.GetAllProductsAsync();
    return Results.Ok(produtos);
});

// Método para consultar um produto a partir do seu Id
app.MapGet("/produtos/{id}", async (int id, ProductService productService) =>
{
    var produto = await productService.GetProductByIdAsync(id);
    if (produto == null)
    {
        return Results.NotFound($"Product with ID {id} not found.");
    }
    return Results.Ok(produto);
});

// Método para atualizar os dados de um produto
app.MapPut("/produtos/{id}", async (int id, Produto produto, ProductService productService) =>
{
    if (id != produto.Id)
    {
        return Results.BadRequest("Product ID mismatch.");
    }
    await productService.UpdateProductAsync(produto);
    return Results.Ok();
});

// Método para excluir um produto
app.MapDelete("/produtos/{id}", async (int id, ProductService productService) =>
{
    await productService.DeleteProductAsync(id);
    return Results.Ok();
});

//<<<----------Cliente----------->>>

app.MapPost("/createcliente", async (LojaDbContext dbContext, Cliente newCliente) =>
{
    dbContext.Clientes.Add(newCliente);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/createcliente/{newCliente.Id}", newCliente);
});

app.MapGet("/clientes", async (LojaDbContext dbContext) =>
{
    var clientes = await dbContext.Clientes.ToListAsync();
    return Results.Ok(clientes);
});

app.MapGet("/clientes/{id}", async (int id, LojaDbContext dbContext) =>
{
    var cliente = await dbContext.Clientes.FindAsync(id);
    if (cliente == null)
    {
        return Results.NotFound($"Cliente with ID {id} not found.");
    }
    return Results.Ok(cliente);
});

app.MapPut("/clientes/{id}", async (int id, LojaDbContext dbContext, Cliente updateCliente) =>
{
    var existingCliente = await dbContext.Clientes.FindAsync(id);
    if (existingCliente == null)
    {
        return Results.NotFound($"Cliente with ID {id} not found.");
    }

    existingCliente.Nome = updateCliente.Nome;
    existingCliente.Cpf = updateCliente.Cpf;
    existingCliente.Email = updateCliente.Email;

    await dbContext.SaveChangesAsync();

    return Results.Ok(existingCliente);
});

//<<<----------Fornecedor----------->>>

// Método para gravar um novo fornecedor
app.MapPost("/createfornecedor", async (Fornecedor fornecedor, FornecedorService fornecedorService) =>
{
    await fornecedorService.AddFornecedorAsync(fornecedor);
    return Results.Created($"/fornecedores/{fornecedor.Id}", fornecedor);
});

// Método para consultar todos os fornecedores
app.MapGet("/fornecedores", async (FornecedorService fornecedorService) =>
{
    var fornecedores = await fornecedorService.GetAllFornecedoresAsync();
    return Results.Ok(fornecedores);
});

// Método para consultar um fornecedor a partir do seu Id
app.MapGet("/fornecedores/{id}", async (int id, FornecedorService fornecedorService) =>
{
    var fornecedor = await fornecedorService.GetFornecedorByIdAsync(id);
    if (fornecedor == null)
    {
        return Results.NotFound($"Fornecedor with ID {id} not found.");
    }
    return Results.Ok(fornecedor);
});

// Método para atualizar os dados de um fornecedor
app.MapPut("/fornecedores/{id}", async (int id, Fornecedor fornecedor, FornecedorService fornecedorService) =>
{
    if (id != fornecedor.Id)
    {
        return Results.BadRequest("Fornecedor ID mismatch.");
    }
    await fornecedorService.UpdateFornecedorAsync(fornecedor);
    return Results.Ok();
});

// Método para excluir um fornecedor
app.MapDelete("/fornecedores/{id}", async (int id, FornecedorService fornecedorService) =>
{
    await fornecedorService.DeleteFornecedorAsync(id);
    return Results.Ok();
});

//<<<----------Usuários----------->>>

// Método para gravar um novo usuário
app.MapPost("/createusuario", async (Usuario usuario, UsuarioService usuarioService) =>
{
    await usuarioService.AddUsuarioAsync(usuario);
    return Results.Created($"/usuarios/{usuario.Id}", usuario);
});

// Método para consultar todos os usuários
app.MapGet("/usuarios", async (UsuarioService usuarioService) =>
{
    var usuarios = await usuarioService.GetAllUsuariosAsync();
    return Results.Ok(usuarios);
});

// Método para consultar um usuário a partir do seu Id
app.MapGet("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    var usuario = await usuarioService.GetUsuarioByIdAsync(id);
    if (usuario == null)
    {
        return Results.NotFound($"Usuario with ID {id} not found.");
    }
    return Results.Ok(usuario);
});

// Método para atualizar os dados de um usuário
app.MapPut("/usuarios/{id}", async (int id, Usuario usuario, UsuarioService usuarioService) =>
{
    if (id != usuario.Id)
    {
        return Results.BadRequest("Usuario ID mismatch.");
    }
    await usuarioService.UpdateUsuarioAsync(usuario);
    return Results.Ok();
});

// Método para excluir um usuário
app.MapDelete("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    await usuarioService.DeleteUsuarioAsync(id);
    return Results.Ok();
});

//<<<----------Vendas----------->>>

// Gravar uma venda
app.MapPost("/createvenda", async (Venda venda, VendaService vendaService, ProductService productService, LojaDbContext dbContext) =>
{
    var cliente = await dbContext.Clientes.FindAsync(venda.ClienteId);
    var produto = await dbContext.Produtos.FindAsync(venda.ProdutoId);

    if (cliente == null || produto == null)
    {
        return Results.BadRequest("Cliente ou produto não encontrado.");
    }

    venda.Cliente = cliente;
    venda.Produto = produto;
    await vendaService.AddVendaAsync(venda);
    return Results.Created($"/vendas/{venda.Id}", venda);
});

// Consultar vendas por produto (detalhada)
app.MapGet("/vendas/produto/{produtoId}", async (int produtoId, VendaService vendaService) =>
{
    var vendas = await vendaService.GetVendasByProdutoIdAsync(produtoId);
    var result = vendas.Select(v => new
    {
        ProdutoNome = v.Produto.Nome,
        DataVenda = v.DataVenda,
        VendaId = v.Id,
        ClienteNome = v.Cliente.Nome,
        QuantidadeVendida = v.Quantidade,
        PrecoVenda = v.PrecoUnitario
    });
    return Results.Ok(result);
});

// Consultar vendas por produto (sumarizada)
app.MapGet("/vendas/produto/sum/{produtoId}", async (int produtoId, VendaService vendaService) =>
{
    var vendas = await vendaService.GetVendasByProdutoIdAsync(produtoId);
    var result = new
    {
        ProdutoNome = vendas.First().Produto.Nome,
        TotalQuantidadeVendida = vendas.Sum(v => v.Quantidade),
        TotalPrecoVenda = vendas.Sum(v => v.PrecoUnitario * v.Quantidade)
    };
    return Results.Ok(result);
});

// Consultar vendas por cliente (detalhada)
app.MapGet("/vendas/cliente/{clienteId}", async (int clienteId, VendaService vendaService) =>
{
    var vendas = await vendaService.GetVendasByClienteIdAsync(clienteId);
    var result = vendas.Select(v => new
    {
        ProdutoNome = v.Produto.Nome,
        DataVenda = v.DataVenda,
        VendaId = v.Id,
        QuantidadeVendida = v.Quantidade,
        PrecoVenda = v.PrecoUnitario
    });
    return Results.Ok(result);
});

// Consultar vendas por cliente (sumarizada)
app.MapGet("/vendas/cliente/sum/{clienteId}", async (int clienteId, VendaService vendaService) =>
{
    var vendas = await vendaService.GetVendasByClienteIdAsync(clienteId);
    var result = new
    {
        ClienteNome = vendas.First().Cliente.Nome,
        TotalQuantidadeVendida = vendas.Sum(v => v.Quantidade),
        TotalPrecoVenda = vendas.Sum(v => v.PrecoUnitario * v.Quantidade)
    };
    return Results.Ok(result);
});


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

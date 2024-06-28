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

// -- Configuração de conexão com o BD
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LojaDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// -- Configuração da autenticação JWT
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

// -- Adicionar as services
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<ClienteService>();

// -- Adicionar serviços do Swagger ao contêiner
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Loja API", Version = "v1" });
});

// -- Adicionar serviços de autorização
builder.Services.AddAuthorization();

var app = builder.Build();

// -- Middleware para roteamento
app.UseRouting();

// -- Middleware para autenticação
app.UseAuthentication();

// -- Middleware para autorização
app.UseAuthorization();

// -- Rota protegida com verificação manual do token (sem uso do JWT Middleware)
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

    // Valida o token manualmente
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
}).RequireAuthorization(); // Requer autenticação JWT

// Rota segura com verificação manual do token (sem uso do JWT Middleware)
app.MapGet("/rotaSegura", async (HttpContext context) =>
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

    // Valida o token manualmente
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes("abc");
    try
    {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
        }, out _);
        await context.Response.WriteAsync("Acesso autorizado");
    }
    catch
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Token inválido");
    }
}).RequireAuthorization(); // Requer autenticação JWT

// -- Endpoint de Login
// -- Endpoint de Login
app.MapPost("/login", async (HttpContext context) =>
{
    // Receber o request
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    // Deserializar o objeto
    var json = JsonDocument.Parse(body);
    var email = json.RootElement.GetProperty("email").GetString();
    var senha = json.RootElement.GetProperty("senha").GetString();

    // Lógica de validação de usuário e geração de token JWT
    var token = "";
    if (senha == "1029") // Exemplo de validação de senha (substitua pela sua lógica real de autenticação)
    {
        // Aqui você pode chamar um serviço de autenticação ou validar as credenciais de outra forma
        // Exemplo simples: apenas verifica se a senha é "1029" (não recomendado para produção)
        token = GenerateToken(email);
    }
    else
    {
        // Caso as credenciais sejam inválidas, retorne um código de status 401 Unauthorized
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Credenciais inválidas");
        return;
    }

    // Retorna o token JWT gerado
    await context.Response.WriteAsync(token);
});

// -- Método para gerar o token (deve ser movido para uma classe separada posteriormente)
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




//----------------------------Endpoints-------------------------------->


//<<<----------Cliente----------->>>

// -- Método para criar um novo cliente
app.MapPost("/createcliente", async (ClienteService clienteService, Cliente newCliente) =>
{
    await clienteService.AddClienteAsync(newCliente);
    return Results.Created($"/createcliente/{newCliente.Id}", newCliente);
});

// -- Método para consultar todos os clientes
app.MapGet("/clientes", async (ClienteService clienteService) =>
{
    var clientes = await clienteService.GetAllClientesAsync();
    return Results.Ok(clientes);
});

// -- Método para consultar um cliente a partir do seu Id
app.MapGet("/clientes/{id}", async (int id, ClienteService clienteService) =>
{
    var cliente = await clienteService.GetClienteByIdAsync(id);
    if (cliente == null)
    {
        return Results.NotFound($"Cliente with ID {id} not found.");
    }
    return Results.Ok(cliente);
});

// -- Método para atualizar os dados de um cliente
app.MapPut("/clientes/{id}", async (int id, ClienteService clienteService, Cliente updateCliente) =>
{
    var existingCliente = await clienteService.GetClienteByIdAsync(id);
    if (existingCliente == null)
    {
        return Results.NotFound($"Cliente with ID {id} not found.");
    }

    existingCliente.Nome = updateCliente.Nome;
    existingCliente.Cpf = updateCliente.Cpf;
    existingCliente.Email = updateCliente.Email;

    await clienteService.UpdateClienteAsync(existingCliente);

    return Results.Ok(existingCliente);
});

// -- Método para excluir um cliente
app.MapDelete("/clientes/{id}", async (int id, ClienteService clienteService) =>
{
    await clienteService.DeleteClienteAsync(id);
    return Results.Ok();
});

//<<<----------Servicos----------->>>
/// -- Método para criar um novo serviço
app.MapPost("/servicos", async (Servico novoServico, ServicoService servicoService) =>
{
    await servicoService.AddServicoAsync(novoServico);
    return Results.Created($"/servicos/{novoServico.Id}", novoServico);
});

// -- Método para consultar todos os serviços
app.MapGet("/servicos", async (ServicoService servicoService) =>
{
    var servicos = await servicoService.GetAllServicosAsync();
    return Results.Ok(servicos);
});

// -- Método para consultar um serviço a partir do seu Id
app.MapGet("/servicos/{id}", async (int id, ServicoService servicoService) =>
{
    var servico = await servicoService.GetServicoByIdAsync(id);
    if (servico == null)
    {
        return Results.NotFound($"Serviço com ID {id} não encontrado.");
    }
    return Results.Ok(servico);
});

// -- Método para atualizar os dados de um serviço
app.MapPut("/servicos/{id}", async (int id, Servico servicoAtualizado, ServicoService servicoService) =>
{
    var servicoExistente = await servicoService.GetServicoByIdAsync(id);
    if (servicoExistente == null)
    {
        return Results.NotFound($"Serviço com ID {id} não encontrado.");
    }

    servicoExistente.Nome = servicoAtualizado.Nome;
    servicoExistente.Preco = servicoAtualizado.Preco;
    servicoExistente.Status = servicoAtualizado.Status;

    await servicoService.UpdateServicoAsync(servicoExistente);

    return Results.Ok(servicoExistente);
});

// -- Método para excluir um serviço
app.MapDelete("/servicos/{id}", async (int id, ServicoService servicoService) =>
{
    await servicoService.DeleteServicoAsync(id);
    return Results.Ok();


});






//<<<----------Usuários----------->>>

// -- Método para gravar um novo usuário
app.MapPost("/createusuario", async (Usuario usuario, UsuarioService usuarioService) =>
{
    await usuarioService.AddUsuarioAsync(usuario);
    return Results.Created($"/usuarios/{usuario.Id}", usuario);
});

// -- Método para consultar todos os usuários
app.MapGet("/usuarios", async (UsuarioService usuarioService) =>
{
    var usuarios = await usuarioService.GetAllUsuariosAsync();
    return Results.Ok(usuarios);
});

// -- Método para consultar um usuário a partir do seu Id
app.MapGet("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    var usuario = await usuarioService.GetUsuarioByIdAsync(id);
    if (usuario == null)
    {
        return Results.NotFound($"Usuario with ID {id} not found.");
    }
    return Results.Ok(usuario);
});

// -- Método para atualizar os dados de um usuário
app.MapPut("/usuarios/{id}", async (int id, Usuario usuario, UsuarioService usuarioService) =>
{
    if (id != usuario.Id)
    {
        return Results.BadRequest("Usuario ID mismatch.");
    }
    await usuarioService.UpdateUsuarioAsync(usuario);
    return Results.Ok();
});

// -- Método para excluir um usuário
app.MapDelete("/usuarios/{id}", async (int id, UsuarioService usuarioService) =>
{
    await usuarioService.DeleteUsuarioAsync(id);
    return Results.Ok();
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

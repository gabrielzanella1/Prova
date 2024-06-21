using Microsoft.EntityFrameworkCore;
using Loja.data;
using Loja.models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar conexão com o BD
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LojaDbContext>(options => options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



//<<<----------Produtos----------->>>

app.MapPost("/createproduto", async (LojaDbContext dbContext, Produto newProduto) => 
{
    dbContext.Produtos.Add(newProduto);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/createproduto/{newProduto.Id}", newProduto);
});

app.MapGet("/produtos", async (LojaDbContext dbContext) => 
{
    var produtos = await dbContext.Produtos.ToListAsync();
    return Results.Ok(produtos);
});

app.MapGet("/produtos/{id}", async (int id, LojaDbContext dbContext) => 
{
    var produto = await dbContext.Produtos.FindAsync(id);
    if (produto == null){
        return Results.NotFound($"Produto with ID {id} not found.");
    }
    return Results.Ok(produto);
});

app.MapPut("/produtos/{id}", async (int id, LojaDbContext dbContext, Produto updateProduto) =>
{

    //Verifica se o produto existe no banco de dados e caso exista será retornado para dentro do objeto existingProduto.
    var existingProduto = await dbContext.Produtos.FindAsync(id);
    if(existingProduto == null){
        return Results.NotFound($"Produto with ID {id} not found.");
    }

    //Atualiza os dados do existingProduto.
    existingProduto.Nome = updateProduto.Nome;
    existingProduto.Preco = updateProduto.Preco;
    existingProduto.Fornecedor = updateProduto.Fornecedor;

    //Salva no bd
    await dbContext.SaveChangesAsync();

    //Retorna para o cliente que invocou o endpoint.
    return Results.Ok(existingProduto);
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
    if (cliente == null){
        return Results.NotFound($"Cliente with ID {id} not found.");
    }
    return Results.Ok(cliente);
});

app.MapPut("/clientes/{id}", async (int id, LojaDbContext dbContext, Cliente updateCliente) =>
{

    //Verifica se o cliente existe no banco de dados e caso exista será retornado para dentro do objeto existingProduto.
    var existingCliente = await dbContext.Clientes.FindAsync(id);
    if(existingCliente == null){
        return Results.NotFound($"Cliente with ID {id} not found.");
    }

    //Atualiza os dados do existingCliente.
    existingCliente.Nome = updateCliente.Nome;
    existingCliente.Cpf = updateCliente.Cpf;
    existingCliente.Email = updateCliente.Email;

    //Salva no bd
    await dbContext.SaveChangesAsync();

    //Retorna para o cliente que invocou o endpoint.
    return Results.Ok(existingCliente);
});

//<<<----------Fornecedor----------->>>

app.MapPost("/createfornecedor", async (LojaDbContext dbContext, Fornecedor newFornecedor) =>
{
    dbContext.Fornecedores.Add(newFornecedor);
    await dbContext.SaveChangesAsync();
    return Results.Created($"/createfornecedor/{newFornecedor.Id}", newFornecedor);
});

app.MapGet("/fornecedores", async (LojaDbContext dbContext) =>
{
    var fornecedores = await dbContext.Fornecedores.ToListAsync();
    return Results.Ok(fornecedores);
});

app.MapGet("/fornecedores/{Id}", async (int Id, LojaDbContext dbContext) =>
{
    var fornecedor = await dbContext.Fornecedores.FindAsync(Id);
    if (fornecedor == null)
    {
        return Results.NotFound($"Fornecedor de ID {Id} não encontrado");
    }

    return Results.Ok(fornecedor);
});

app.MapPut("/fornecedores/{Id}", async (int Id, LojaDbContext dbContext, Fornecedor updateFornecedor) =>
{
    var existingFornecedor = await dbContext.Fornecedores.FindAsync(Id);
    if (existingFornecedor == null)
    {
        return Results.NotFound($"Fornecedor de ID {Id} não encontrado");
    }

    existingFornecedor.Nome = updateFornecedor.Nome;
    existingFornecedor.CNPJ = updateFornecedor.CNPJ;
    existingFornecedor.Endereco = updateFornecedor.Endereco;
    existingFornecedor.Email = updateFornecedor.Email;
    existingFornecedor.Telefone = updateFornecedor.Telefone;

    await dbContext.SaveChangesAsync();

    return Results.Ok(existingFornecedor);
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

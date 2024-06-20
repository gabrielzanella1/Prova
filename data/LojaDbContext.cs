using Loja.models;
using Microsoft.EntityFrameworkCore;

namespace Loja.data
{
    public class LojaDbContext : DbContext
    {
        public LojaDbContext(DbContextOptions<LojaDbContext> options) : base(options) { }
        
        public DbSet<Produto> Produtos { get; set; }
    }
}

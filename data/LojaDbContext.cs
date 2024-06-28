using Microsoft.EntityFrameworkCore;
using Loja.models;

namespace Loja.data
{
    public class LojaDbContext : DbContext
    {
        public LojaDbContext(DbContextOptions<LojaDbContext> options) : base(options) { }

        public DbSet<Servico> Servicos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
    }
}

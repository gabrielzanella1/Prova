using Loja.data;
using Loja.models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Loja.services
{
    public class VendaService
    {
        private readonly LojaDbContext _context;

        public VendaService(LojaDbContext context)
        {
            _context = context;
        }

        public async Task AddVendaAsync(Venda venda)
        {
            _context.Vendas.Add(venda);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Venda>> GetVendasByProdutoIdAsync(int produtoId)
        {
            return await _context.Vendas
                .Include(v => v.Cliente)
                .Include(v => v.Produto)
                .Where(v => v.ProdutoId == produtoId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Venda>> GetVendasByClienteIdAsync(int clienteId)
        {
            return await _context.Vendas
                .Include(v => v.Cliente)
                .Include(v => v.Produto)
                .Where(v => v.ClienteId == clienteId)
                .ToListAsync();
        }
    }
}

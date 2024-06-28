using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Loja.data;
using Loja.models;

namespace Loja.services
{
    public class ServicoService
    {
        private readonly LojaDbContext _dbContext;

        public ServicoService(LojaDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Servico>> GetAllServicosAsync()
        {
            return await _dbContext.Servicos.ToListAsync();
        }

        public async Task<Servico> GetServicoByIdAsync(int id)
        {
            return await _dbContext.Servicos.FindAsync(id);
        }

        public async Task AddServicoAsync(Servico servico)
        {
            _dbContext.Servicos.Add(servico);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateServicoAsync(Servico servico)
        {
            _dbContext.Servicos.Update(servico);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteServicoAsync(int id)
        {
            var servico = await _dbContext.Servicos.FindAsync(id);
            if (servico != null)
            {
                _dbContext.Servicos.Remove(servico);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}

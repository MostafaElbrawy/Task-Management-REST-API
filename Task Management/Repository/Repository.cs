
using Microsoft.EntityFrameworkCore;
using Task_Management.Models;

namespace Task_Management.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationContext _context;
        protected DbSet<T> _dbSet { get; set; }

        public Repository(ApplicationContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public IQueryable<T> GetAll()
        {
            return _dbSet.AsQueryable();
        }
        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async Task<bool> AddAsync(T entity)
        {
            if (entity == null) return false;

            await _dbSet.AddAsync(entity);
            return true;
        }
        public async Task<bool> UpdateAsync( T entity)
        {
            if (entity == null) return false;

            _dbSet.Update(entity);
            return true;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            T? entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                return true;
            }
            return false;
        }

    }
}

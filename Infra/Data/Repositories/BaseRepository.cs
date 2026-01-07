using Microsoft.EntityFrameworkCore;
using MyVocaList.Infra.Utils;
using System.Linq.Expressions;

namespace MyVocaList.Infra.Data.Repositories
{
    /// <summary>
    /// Base repository implementation for common database operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(AppDbContext context)
        {
            Guard.AgainstNull(context, nameof(context));

            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            Guard.AgainstNegativeOrZero(id, nameof(id));

            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            Guard.AgainstNull(predicate, nameof(predicate));

            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            Guard.AgainstNull(entity, nameof(entity));

            await _dbSet.AddAsync(entity);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            Guard.AgainstNull(entity, nameof(entity));

            _dbSet.Update(entity);
        }

        public virtual async Task DeleteAsync(T entity)
        {
            Guard.AgainstNull(entity, nameof(entity));

            _dbSet.Remove(entity);
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            Guard.AgainstNullOrEmpty(entities, nameof(entities));

            _dbSet.RemoveRange(entities);
            // RemoveRange is a synchronous in-memory operation on DbContext.
            // The subsequent SaveChangesAsync will persist all deletions to the database.
            return Task.CompletedTask;
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

namespace Task_Management.Repository
{
    public interface IRepository<T>
    {
        IQueryable<T> GetAll();
        Task<T?> GetByIdAsync(int id);
        Task<bool> AddAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(int id);
    }
}

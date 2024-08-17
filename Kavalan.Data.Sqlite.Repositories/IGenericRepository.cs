
namespace Kavalan.Data.Sqlite.Repositories
{
    public interface IGenericRepository<T> where T : new()
    {
        Task<T?> SelectByFieldValueAsync(string fieldName, object fieldValue);
        Task<T?> SelectByPrimaryKeyAsync(object pkValue);
        Task<List<T>> SelectDataByFieldValueAsync(string fieldName = "", object? fieldValue = null);
        Task<T> InsertAsync(T entity);
        Task<T> UpsertAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<bool> AnyAsync();
        Task<long> CountAsync();
    }
}

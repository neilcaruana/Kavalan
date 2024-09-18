
namespace Kavalan.Data.Sqlite.Repositories
{
    public interface IGenericRepository<T> where T : new()
    {
        Task<T?> SelectByFieldValueAsync(string fieldName, object fieldValue);
        Task<T?> SelectByPrimaryKeyAsync(object[] primaryKeyValues);
        Task<T?> SelectByExpressionAsync(string whereClause = "", string orderByCaluse = "", int limit = -1);
        Task<List<T>> SelectDataByFieldValueAsync(string fieldName = "", object? fieldValue = null);
        Task<List<T>> SelectDataByExpressionAsync(string fieldName = "", object? fieldValue = null, string whereClause = "", string orderByCaluse = "");
        Task<T> InsertAsync(T entity);
        Task<T> UpsertAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<bool> AnyAsync(string whereClause = "");
        Task<long> CountAsync(string whereClause = "");
    }
}

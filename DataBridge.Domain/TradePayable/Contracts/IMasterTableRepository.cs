namespace DataBridge.Domain.TradePayable.Contracts;

public interface IMasterTableRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task UpsertAsync(T record);
    Task DeleteAsync(int id);
}

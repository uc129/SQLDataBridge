namespace DataBridge.Domain.TradePayable.Contracts;

public interface ICrossServerPORepository
{
    /// <summary>Returns PO number → credit period (days) mapping.</summary>
    Task<Dictionary<string, string>> GetPOCreditPeriodsAsync();
}

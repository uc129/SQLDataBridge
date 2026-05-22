using Domain.Aggregates;

namespace Application.Services.MasterTableServices
{
    public partial class StaticMasterTableService
    {
        public async Task<IEnumerable<MSMECompanyCodes>> GetMSMECCData()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all MSMECC data.");
            var data = await _msmeccrepo.GetAllAsync();
            return data;
        }
    }
}

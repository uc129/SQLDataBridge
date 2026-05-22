using Domain.Aggregates;
using Domain.Contracts;
using Infrastructure.Contracts;
using Microsoft.Extensions.Logging;
using System.Data;



namespace Application.Services
{
    /// <summary>
    /// Application Service: Implements use cases and coordinates data flow.
    /// </summary>
    public class CrossServermasterTablesService(IPOCreditPeriodRepository pocredrepo, IPOVendorRecordsRepository povendorrepo, IVendorRecordsRepository vendorrepo)
    {

        private readonly IPOCreditPeriodRepository _pocredrepo = pocredrepo;
        private readonly IPOVendorRecordsRepository _povendorrepo = povendorrepo;
        private readonly IVendorRecordsRepository _vendorrepo = vendorrepo;


        // PO Credit Period Records
        public async Task<DataTable> GetAllPOCreditPeriodsAsDataTableAsync()
        {
            return await _pocredrepo.GetAllAsDataTableAsync();
        }

        public async Task<IEnumerable<POCreditPeriod>> GetAllPOCreditPeriodsAsync()
        {
            return await _pocredrepo.GetAllAsync();
        }

        // PO Vendor Records

        public async Task<DataTable> GetAllPOVendorRecordsAsDataTableAsync()
        {
            return await _povendorrepo.GetAllAsDataTableAsync();
        }
        public async Task<IEnumerable<POVendorRecords>> GetAllPOVendorRecordsAsync()
        {
            return await _povendorrepo.GetAllAsync();
        }


        // Vendor Records
        public async Task<DataTable> GetAllVendorRecordsAsDataTableAsync()
        {
            return await _vendorrepo.GetAllAsDataTableAsync();
        }

        public async Task<IEnumerable<VendorRecords>> GetAllVendorRecordsAsync()
        {
            return await _vendorrepo.GetAllAsync();
        }
    }
}

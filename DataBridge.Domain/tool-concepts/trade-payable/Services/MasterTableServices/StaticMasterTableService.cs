// File: StaticMasterTableService.cs
using Domain.Aggregates;
using Infrastructure.Contracts;

namespace Application.Services.MasterTableServices
{
    public partial class StaticMasterTableService(
        IAdvanceGLsRepository advanceglrepo,
        IAgeingGroupRepository ageinggrouprepo,
        ICapitalCreditorGLsRepository capitalcreditorglsrepo,
        IInsuranceGLsRepository insuranceglsrepo,
        ILiabilityGLsRepository liabilityglsrepo,
        IMSMECCRepository msmeccrepo,
        INonMSMEGlsRepository nonmsmeglsrepo,
        INotDueGLsRepository notdueglsrepo,
        IUnclaimedGlsRepository unclaimedglsrepo,
        IICPHyperionMapRepository icphyperionmaprepo,
        IICPVendorMapRepository icpvendormaprepo,
        IForexMonthEndMapRepository forexmonthendmaprepo,
        IGLHyperionMapRepository glhyperionmaprepo

    )
    {
        private readonly IAdvanceGLsRepository _advanceglrepo = advanceglrepo;
        private readonly IAgeingGroupRepository _ageinggrouprepo = ageinggrouprepo;
        private readonly ICapitalCreditorGLsRepository _capitalcreditorglsrepo = capitalcreditorglsrepo;
        private readonly IInsuranceGLsRepository _insuranceglsrepo = insuranceglsrepo;
        private readonly ILiabilityGLsRepository _liabilityglsrepo = liabilityglsrepo;
        private readonly IMSMECCRepository _msmeccrepo = msmeccrepo;
        private readonly INonMSMEGlsRepository _nonmsmeglsrepo = nonmsmeglsrepo;
        private readonly INotDueGLsRepository _notdueglsrepo = notdueglsrepo;
        private readonly IUnclaimedGlsRepository _unclaimedglsrepo = unclaimedglsrepo;
        private readonly IICPHyperionMapRepository _icphyperionmaprepo = icphyperionmaprepo;
        private readonly IICPVendorMapRepository _icpvendormaprepo = icpvendormaprepo;
        private readonly IForexMonthEndMapRepository _forexmonthendmaprepo = forexmonthendmaprepo;
        private readonly IGLHyperionMapRepository _glhyperionmaprepo = glhyperionmaprepo;

        public async Task<IEnumerable<AgeingGroup>> GetAgeingGroups()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all AdvanceGLs data.");
            var data = await _ageinggrouprepo.GetAllAsync();
            return data;
        }
        
    }
}
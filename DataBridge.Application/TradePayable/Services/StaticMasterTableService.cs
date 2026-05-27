using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;

namespace DataBridge.Application.TradePayable.Services;

public class StaticMasterTableService(
    IMasterTableRepository<AdvanceGLs>       advanceGLsRepo,
    IMasterTableRepository<LiabilityGLs>     liabilityGLsRepo,
    IMasterTableRepository<NotDueGLs>        notDueGLsRepo,
    IMasterTableRepository<MSMECompanyCodes> msmeCodesRepo,
    IMasterTableRepository<CapitalCreditorGLs> capitalGLsRepo,
    IMasterTableRepository<InsuranceGLs>     insuranceGLsRepo,
    IMasterTableRepository<NonMSMEGLs>       nonMsmeGLsRepo,
    IMasterTableRepository<UnclaimedGLs>     unclaimedGLsRepo,
    IMasterTableRepository<AgeingGroup>      ageingGroupRepo,
    IMasterTableRepository<ICPVendorMap>     icpVendorMapRepo,
    IMasterTableRepository<ICPHyperionMap>   icpHyperionMapRepo,
    IMasterTableRepository<ForexMonthEndMap> forexRepo,
    IMasterTableRepository<GLHyperionMap>    glHyperionRepo)
{
    public Task<IEnumerable<AdvanceGLs>>       GetAdvanceGLsAsync()       => advanceGLsRepo.GetAllAsync();
    public Task<IEnumerable<LiabilityGLs>>     GetLiabilityGLsAsync()     => liabilityGLsRepo.GetAllAsync();
    public Task<IEnumerable<NotDueGLs>>        GetNotDueGLsAsync()        => notDueGLsRepo.GetAllAsync();
    public Task<IEnumerable<MSMECompanyCodes>> GetMSMECodesAsync()        => msmeCodesRepo.GetAllAsync();
    public Task<IEnumerable<CapitalCreditorGLs>> GetCapitalGLsAsync()     => capitalGLsRepo.GetAllAsync();
    public Task<IEnumerable<InsuranceGLs>>     GetInsuranceGLsAsync()     => insuranceGLsRepo.GetAllAsync();
    public Task<IEnumerable<NonMSMEGLs>>       GetNonMSMEGLsAsync()       => nonMsmeGLsRepo.GetAllAsync();
    public Task<IEnumerable<UnclaimedGLs>>     GetUnclaimedGLsAsync()     => unclaimedGLsRepo.GetAllAsync();
    public Task<IEnumerable<AgeingGroup>>      GetAgeingGroupsAsync()     => ageingGroupRepo.GetAllAsync();
    public Task<IEnumerable<ICPVendorMap>>     GetICPVendorMapAsync()     => icpVendorMapRepo.GetAllAsync();
    public Task<IEnumerable<ICPHyperionMap>>   GetICPHyperionMapAsync()   => icpHyperionMapRepo.GetAllAsync();
    public Task<IEnumerable<ForexMonthEndMap>> GetForexMonthEndMapAsync() => forexRepo.GetAllAsync();
    public Task<IEnumerable<GLHyperionMap>>    GetGLHyperionMapAsync()    => glHyperionRepo.GetAllAsync();
}

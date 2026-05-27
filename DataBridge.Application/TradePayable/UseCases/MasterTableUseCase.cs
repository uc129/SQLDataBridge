using DataBridge.Application.TradePayable.Services;
using DataBridge.Domain.TradePayable.Contracts;
using DataBridge.Domain.TradePayable.MasterTables;

namespace DataBridge.Application.TradePayable.UseCases;

public class MasterTableUseCase(
    StaticMasterTableService                 masterSvc,
    IMasterTableRepository<AdvanceGLs>       advanceGLsRepo,
    IMasterTableRepository<LiabilityGLs>     liabilityGLsRepo,
    IMasterTableRepository<NotDueGLs>        notDueGLsRepo,
    IMasterTableRepository<MSMECompanyCodes> msmeCodesRepo,
    IMasterTableRepository<CapitalCreditorGLs> capitalGLsRepo,
    IMasterTableRepository<InsuranceGLs>     insuranceGLsRepo,
    IMasterTableRepository<NonMSMEGLs>       nonMsmeGLsRepo,
    IMasterTableRepository<UnclaimedGLs>     unclaimedGLsRepo,
    IMasterTableRepository<GLHyperionMap>    glHyperionRepo,
    IMasterTableRepository<ICPHyperionMap>   icpHyperionRepo,
    IMasterTableRepository<ICPVendorMap>     icpVendorRepo,
    IMasterTableRepository<ForexMonthEndMap> forexRepo,
    IMasterTableRepository<AgeingGroup>      ageingGroupRepo)
{
    // ── AdvanceGLs ───────────────────────────────────────────────────────────
    public Task<IEnumerable<AdvanceGLs>>       GetAdvanceGLsAsync()           => masterSvc.GetAdvanceGLsAsync();
    public Task                                UpsertAdvanceGLAsync(AdvanceGLs r) => advanceGLsRepo.UpsertAsync(r);
    public Task                                DeleteAdvanceGLAsync(int id)    => advanceGLsRepo.DeleteAsync(id);

    // ── LiabilityGLs ─────────────────────────────────────────────────────────
    public Task<IEnumerable<LiabilityGLs>>     GetLiabilityGLsAsync()             => masterSvc.GetLiabilityGLsAsync();
    public Task                                UpsertLiabilityGLAsync(LiabilityGLs r) => liabilityGLsRepo.UpsertAsync(r);
    public Task                                DeleteLiabilityGLAsync(int id)      => liabilityGLsRepo.DeleteAsync(id);

    // ── NotDueGLs ────────────────────────────────────────────────────────────
    public Task<IEnumerable<NotDueGLs>>        GetNotDueGLsAsync()               => masterSvc.GetNotDueGLsAsync();
    public Task                                UpsertNotDueGLAsync(NotDueGLs r)   => notDueGLsRepo.UpsertAsync(r);
    public Task                                DeleteNotDueGLAsync(int id)        => notDueGLsRepo.DeleteAsync(id);

    // ── MSMECompanyCodes ─────────────────────────────────────────────────────
    public Task<IEnumerable<MSMECompanyCodes>> GetMSMECodesAsync()                 => masterSvc.GetMSMECodesAsync();
    public Task                                UpsertMSMECodeAsync(MSMECompanyCodes r) => msmeCodesRepo.UpsertAsync(r);
    public Task                                DeleteMSMECodeAsync(int id)          => msmeCodesRepo.DeleteAsync(id);

    // ── CapitalCreditorGLs ───────────────────────────────────────────────────
    public Task<IEnumerable<CapitalCreditorGLs>> GetCapitalGLsAsync()                  => masterSvc.GetCapitalGLsAsync();
    public Task                                  UpsertCapitalGLAsync(CapitalCreditorGLs r) => capitalGLsRepo.UpsertAsync(r);
    public Task                                  DeleteCapitalGLAsync(int id)            => capitalGLsRepo.DeleteAsync(id);

    // ── InsuranceGLs ─────────────────────────────────────────────────────────
    public Task<IEnumerable<InsuranceGLs>>     GetInsuranceGLsAsync()                => masterSvc.GetInsuranceGLsAsync();
    public Task                                UpsertInsuranceGLAsync(InsuranceGLs r) => insuranceGLsRepo.UpsertAsync(r);
    public Task                                DeleteInsuranceGLAsync(int id)         => insuranceGLsRepo.DeleteAsync(id);

    // ── NonMSMEGLs ───────────────────────────────────────────────────────────
    public Task<IEnumerable<NonMSMEGLs>>       GetNonMSMEGLsAsync()               => masterSvc.GetNonMSMEGLsAsync();
    public Task                                UpsertNonMSMEGLAsync(NonMSMEGLs r) => nonMsmeGLsRepo.UpsertAsync(r);
    public Task                                DeleteNonMSMEGLAsync(int id)        => nonMsmeGLsRepo.DeleteAsync(id);

    // ── UnclaimedGLs ─────────────────────────────────────────────────────────
    public Task<IEnumerable<UnclaimedGLs>>     GetUnclaimedGLsAsync()                => masterSvc.GetUnclaimedGLsAsync();
    public Task                                UpsertUnclaimedGLAsync(UnclaimedGLs r) => unclaimedGLsRepo.UpsertAsync(r);
    public Task                                DeleteUnclaimedGLAsync(int id)         => unclaimedGLsRepo.DeleteAsync(id);

    // ── GLHyperionMap ────────────────────────────────────────────────────────
    public Task<IEnumerable<GLHyperionMap>>    GetGLHyperionMapAsync()                 => masterSvc.GetGLHyperionMapAsync();
    public Task                                UpsertGLHyperionMapAsync(GLHyperionMap r) => glHyperionRepo.UpsertAsync(r);
    public Task                                DeleteGLHyperionMapAsync(int id)          => glHyperionRepo.DeleteAsync(id);

    // ── ICPHyperionMap ───────────────────────────────────────────────────────
    public Task<IEnumerable<ICPHyperionMap>>   GetICPHyperionMapAsync()                   => masterSvc.GetICPHyperionMapAsync();
    public Task                                UpsertICPHyperionMapAsync(ICPHyperionMap r) => icpHyperionRepo.UpsertAsync(r);
    public Task                                DeleteICPHyperionMapAsync(int id)           => icpHyperionRepo.DeleteAsync(id);

    // ── ICPVendorMap ─────────────────────────────────────────────────────────
    public Task<IEnumerable<ICPVendorMap>>     GetICPVendorMapAsync()                  => masterSvc.GetICPVendorMapAsync();
    public Task                                UpsertICPVendorMapAsync(ICPVendorMap r) => icpVendorRepo.UpsertAsync(r);
    public Task                                DeleteICPVendorMapAsync(int id)         => icpVendorRepo.DeleteAsync(id);

    // ── ForexMonthEndMap ─────────────────────────────────────────────────────
    public Task<IEnumerable<ForexMonthEndMap>> GetForexMonthEndMapAsync()                    => masterSvc.GetForexMonthEndMapAsync();
    public Task                                UpsertForexMonthEndMapAsync(ForexMonthEndMap r) => forexRepo.UpsertAsync(r);
    public Task                                DeleteForexMonthEndMapAsync(int id)             => forexRepo.DeleteAsync(id);

    // ── AgeingGroup ──────────────────────────────────────────────────────────
    public Task<IEnumerable<AgeingGroup>>      GetAgeingGroupsAsync()                 => masterSvc.GetAgeingGroupsAsync();
    public Task                                UpsertAgeingGroupAsync(AgeingGroup r)  => ageingGroupRepo.UpsertAsync(r);
    public Task                                DeleteAgeingGroupAsync(int id)         => ageingGroupRepo.DeleteAsync(id);
}

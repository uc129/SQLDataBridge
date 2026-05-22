using Infrastructure.Dapper;
using System.Data;
using Dapper;
using Infrastructure.Contracts.ServiceContracts;
using Domain.Models.ViewModels;

namespace Application.Services
{
    public class FAGLL03Service(DapperContext dbcontext):IFAGLL03Service
    {
        private readonly DapperContext _dbcontext = dbcontext;
        public async Task<Fagll03SettingsViewModel> GetRawFAGL03TableDetailsViaSP()
        {
            using var connection = _dbcontext.CreateConnection("default");
            var model = new Fagll03SettingsViewModel();

            using var multi = await connection.QueryMultipleAsync("SP2_GetFAGLL03RAWTableDetails", commandType: CommandType.StoredProcedure);

            // Set 1: Summaries
            var summaries = (await multi.ReadAsync<dynamic>()).ToList();
            foreach (var s in summaries)
            {
                model.TableSizeMb = s.TableSizeMb; // Global
                model.GlobalTotalRecords += (int)s.TotalRecords;

                model.Revisions.Add(new RevisionStats
                {
                    RevisionNumber = s.RevisionNumber,
                    TotalRecords = s.TotalRecords,
                    TotalLiabilityAmountSAP = s.TotalLiabilityAmountSAP,
                    TotalAdvanceAmountSAP = s.TotalAdvanceAmountSAP
                });
            }

            // Set 2: Mapping Company Codes to correct Revision
            var coCodes = await multi.ReadAsync<dynamic>();
            foreach (var c in coCodes)
                model.Revisions.FirstOrDefault(r => r.RevisionNumber == c.RevisionNumber)?
                     .IncludedCompanyCodes.Add(c.Company_Code);

            // Set 3: Mapping GLs
            var gls = await multi.ReadAsync<dynamic>();
            foreach (var g in gls)
            {
                var rev = model.Revisions.FirstOrDefault(r => r.RevisionNumber == g.RevisionNumber);
                if (rev != null)
                {
                    rev.IncludedGLCodes.Add(g.GL_Account);
                    if (g.GLType == "Advance") rev.IncludedAdvanceGLs.Add(g.GL_Account);
                    else rev.IncludedLiabilityGLs.Add(g.GL_Account);
                }
            }

            // Set 4: History
            model.RecentUploads = [.. (await multi.ReadAsync<UploadHistoryItem>())];

            return model;
        }
    }
}

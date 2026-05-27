using System.Data;

namespace DataBridge.Application.TradePayable.Processing;

public class GITProcessor(GITHelper gitHelper, HelperFunctions helper)
{
    /// <summary>
    /// Processes local-currency GIT advance data.
    /// Returns [processedGIT, netLiability, modifiedOriginalWithGroupKeys, allGrouped].
    /// </summary>
    public List<DataTable> ProcessFAGLL03GITData(DataTable mergedData)
    {
        var copy = HelperFunctions.DeepCopyDataTable(mergedData, "copy");

        var withPO    = GITHelper.FilterForDataWithPO(copy);
        var withoutPO = GITHelper.FilterForDataWithoutPO(copy);
        var sna       = GITHelper.FilterForSNAData(copy);

        var groupedWithoutPC = GITHelper.GroupFAGLL03DataWithoutProfitCenter(withPO);
        var groupedWithPC    = GITHelper.GroupFAGLL03DataWithProfitCenter(withoutPO);
        var groupedSNA       = GITHelper.GroupFAGLL03DataForSNA(sna);

        var allGrouped = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(groupedWithoutPC[1], groupedWithPC[1], "PO+NonPO"),
            groupedSNA[1], "All Grouped");

        var modifiedOriginal = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(groupedWithoutPC[0], groupedSNA[0], ""),
            groupedWithPC[0], "Original with group keys");

        var gitWithoutPC = FilterAndProcess(groupedWithoutPC[1], false, false);
        var gitWithPC    = FilterAndProcess(groupedWithPC[1],    true,  false);
        var gitForSNA    = FilterAndProcess(groupedSNA[1],       false, true);

        var processedGIT = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(gitWithoutPC[1], gitWithPC[1], ""),
            gitForSNA[1], "Final ProcessedGIT");

        var netLiability = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(gitWithoutPC[2], gitWithPC[2], ""),
            gitForSNA[2], "Final Net Liability");

        return [processedGIT, netLiability, modifiedOriginal, allGrouped];
    }

    public DataTable AppendTradeAndGITLiabilityTable(DataTable populatedData, DataTable liabilityData) =>
        gitHelper.LineItemWiseAdvanceAdjustment(populatedData, liabilityData);

    private List<DataTable> FilterAndProcess(DataTable data, bool withPC, bool forSNA)
    {
        var liabilityGLs = helper.LiabilityGLs.Select(gl => gl.GL_Code).ToList();
        var advances     = GITHelper.FilterForAdvanceGLs(data);
        var liabilities  = GITHelper.FilterForLiabilityGLs(data, liabilityGLs);

        DataTable rawGIT;
        if (forSNA)
        {
            var pivoted = gitHelper.PivotFAGLL03SNAData(liabilities);
            rawGIT = gitHelper.PerformCascadedJoinSNAData(advances, pivoted);
        }
        else if (withPC)
        {
            var pivoted = gitHelper.PivotLiabilityGLDataWithProfitCenter(liabilities);
            rawGIT = gitHelper.PerformCascadedJoinWithProfitCenter(advances, pivoted);
        }
        else
        {
            var pivoted = gitHelper.PivotLiabilityGLDataWithoutProfitCenter(liabilities);
            rawGIT = gitHelper.PerformCascadedJoinWithoutProfitCenter(advances, pivoted);
        }

        var processed   = GITHelper.GITAdvanceManipulationFAGLL03Data(rawGIT, forSNA);
        var unpivoted   = GITHelper.UnpivotProcessedGIT2(processed);
        return [rawGIT, processed, unpivoted];
    }
}

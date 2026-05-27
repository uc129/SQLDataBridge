using System.Data;

namespace DataBridge.Application.TradePayable.Processing;

public class GITDocCurrProcessor(GITDocCurrHelper docHelper, HelperFunctions helper)
{
    /// <summary>
    /// Processes document-currency GIT advance data.
    /// Returns [processedGIT, netLiability, modifiedOriginalWithGroupKeys, allGrouped].
    /// </summary>
    public List<DataTable> ProcessFAGLL03DocCurrGITData(DataTable mergedData)
    {
        var copy = HelperFunctions.DeepCopyDataTable(mergedData, "copy");

        var withPO    = GITHelper.FilterForDataWithPO(copy);
        var withoutPO = GITHelper.FilterForDataWithoutPO(copy);
        var sna       = GITHelper.FilterForSNAData(copy);

        var groupedWithoutPC = GITDocCurrHelper.GroupFAGLL03DocCurrDataWithoutProfitCenter(withPO);
        var groupedWithPC    = GITDocCurrHelper.GroupFAGLL03DocCurrDataWithProfitCenter(withoutPO);
        var groupedSNA       = GITDocCurrHelper.GroupFAGLL03DocCurrDataForSNA(sna);

        var allGrouped = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(groupedWithoutPC[1], groupedWithPC[1], "PO+NonPO Doc"),
            groupedSNA[1], "All Grouped Doc");

        var modifiedOriginal = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(groupedWithoutPC[0], groupedSNA[0], ""),
            groupedWithPC[0], "Original with doc group keys");

        var gitWithoutPC = FilterAndProcess(groupedWithoutPC[1], false, false);
        var gitWithPC    = FilterAndProcess(groupedWithPC[1],    true,  false);
        var gitForSNA    = FilterAndProcess(groupedSNA[1],       false, true);

        var processedGIT = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(gitWithoutPC[1], gitWithPC[1], ""),
            gitForSNA[1], "Final ProcessedGIT DocCurr");

        var netLiability = HelperFunctions.AppendTablesWithSameColumnNames(
            HelperFunctions.AppendTablesWithSameColumnNames(gitWithoutPC[2], gitWithPC[2], ""),
            gitForSNA[2], "Final Net Liability DocCurr");

        return [processedGIT, netLiability, modifiedOriginal, allGrouped];
    }

    public DataTable AppendTradeAndGITLiabilityDocCurrTable(DataTable populatedData, DataTable liabilityData) =>
        docHelper.DocCurrLineItemWiseAdvanceAdjustment(populatedData, liabilityData);

    private List<DataTable> FilterAndProcess(DataTable data, bool withPC, bool forSNA)
    {
        var liabilityGLs = helper.LiabilityGLs.Select(gl => gl.GL_Code).ToList();
        var advances     = GITHelper.FilterForAdvanceGLs(data);
        var liabilities  = GITHelper.FilterForLiabilityGLs(data, liabilityGLs);

        DataTable rawGIT;
        if (forSNA)
        {
            var pivoted = docHelper.PivotFAGLL03DocCurrSNAData(liabilities);
            rawGIT = docHelper.PerformCascadedJoinDocCurrSNAData(advances, pivoted);
        }
        else if (withPC)
        {
            var pivoted = docHelper.PivotLiabilityGLDocCurrDataWithProfitCenter(liabilities);
            rawGIT = docHelper.PerformDocCurrCascadedJoinWithProfitCenter(advances, pivoted);
        }
        else
        {
            var pivoted = docHelper.PivotLiabilityGLDocCurrDataWithoutProfitCenter(liabilities);
            rawGIT = docHelper.PerformDocCurrCascadedJoinWithoutProfitCenter(advances, pivoted);
        }

        var processed = GITDocCurrHelper.GITAdvanceManipulationFAGLL03DocCurrData(rawGIT, forSNA);
        var unpivoted = GITDocCurrHelper.UnpivotProcessedGIT2(processed);
        return [rawGIT, processed, unpivoted];
    }
}

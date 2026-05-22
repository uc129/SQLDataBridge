using System.Data;


namespace Application.DataProcessor
{
    public class GITProcessor( GITHelper githelper, HelperFunctions helper)
    {
        private readonly GITHelper _githelper = githelper;
        private readonly HelperFunctions _helper = helper;

        public List<DataTable> ProcessFAGLL03GITData(DataTable processedData)
        {
            DataTable processedDataCopy = HelperFunctions.DeepCopyDataTable(processedData, "A copy of Populated Data");

            DataTable DataWithPO = GITHelper.FilterForDataWithPO(processedDataCopy);
            DataTable DataWithoutPO = GITHelper.FilterForDataWithoutPO(processedDataCopy);
            DataTable SNAData = GITHelper.FilterForSNAData(processedDataCopy);


            List<DataTable> DataGroupedWithoutProfitCenter = GITHelper.GroupFAGLL03DataWithoutProfitCenter(DataWithPO);
            List<DataTable> DataGroupedWithProfitCenter = GITHelper.GroupFAGLL03DataWithProfitCenter(DataWithoutPO);
            List<DataTable> GroupedSNAData = GITHelper.GroupFAGLL03DataForSNA(SNAData);

            DataTable allgrouped = HelperFunctions.AppendTablesWithSameColumnNames(DataGroupedWithoutProfitCenter[1], DataGroupedWithProfitCenter[1], "PO,NonPO Grouped Data");
            DataTable AllGLGroupedData = HelperFunctions.AppendTablesWithSameColumnNames(allgrouped, GroupedSNAData[1], "All Grouped Data");


            DataTable modifiedoriginal = HelperFunctions.AppendTablesWithSameColumnNames(DataGroupedWithoutProfitCenter[0], GroupedSNAData[0], "Original data with grouping keys");
            DataTable ModifiedOriginalTable = HelperFunctions.AppendTablesWithSameColumnNames(modifiedoriginal, DataGroupedWithProfitCenter[0], "Original data with grouping keys");



            var GITWithoutPC = FilterAndProcessGroupedGITData(DataGroupedWithoutProfitCenter[1], false, false);
            var GITWithPC = FilterAndProcessGroupedGITData(DataGroupedWithProfitCenter[1], true, false);
            var GITForSNA = FilterAndProcessGroupedGITData(GroupedSNAData[1], false, true);

            //DataTable rawGITSheet = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[0], GITWithPC[0], "Final RawGIT Sheet");
            DataTable processedgit = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[1], GITWithPC[1], "");
            DataTable processedGITData = HelperFunctions.AppendTablesWithSameColumnNames(processedgit, GITForSNA[1], "Final ProcessedGIT Sheet");

            //var ProcessedGITWithVertical= HelperFunctions.AssignCorporateLabel(processedGITData);
            DataTable netliability = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[2], GITWithPC[2], "");
            DataTable netLiabilityTable = HelperFunctions.AppendTablesWithSameColumnNames(netliability, GITForSNA[2], "Final Net Liability Table");

            return [processedGITData, netLiabilityTable, ModifiedOriginalTable, AllGLGroupedData];
        }
        public List<DataTable> FilterAndProcessGroupedGITData(DataTable data, bool withProfitCenter=false, bool forSnaData=false)
        {
            List<string> LiabilityGLs = [.. _helper.LiabilityGLs.Select(gl => gl.GL_Code)];
            if (forSnaData)
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);
                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);
                DataTable RawGITSheet = GITRawSheetDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, false, true);
                DataTable proccessedGITData = GITHelper.GITAdvanceManipulationFAGLL03Data(RawGITSheet, true);
                DataTable liabilityTable = GITHelper.UnpivotProcessedGIT2(proccessedGITData);
                return [RawGITSheet, proccessedGITData, liabilityTable];
            }
            else if(withProfitCenter && !forSnaData)
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);
                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);
                DataTable RawGITSheet = GITRawSheetDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, true, false);
                DataTable proccessedGITData = GITHelper.GITAdvanceManipulationFAGLL03Data(RawGITSheet);
                DataTable liabilityTable = GITHelper.UnpivotProcessedGIT2(proccessedGITData);
                return [RawGITSheet, proccessedGITData, liabilityTable];
            }
            else 
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);
                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);
                DataTable RawGITSheet = GITRawSheetDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, false, false);
                DataTable proccessedGITData = GITHelper.GITAdvanceManipulationFAGLL03Data(RawGITSheet);
                DataTable liabilityTable = GITHelper.UnpivotProcessedGIT2(proccessedGITData);
                return [RawGITSheet, proccessedGITData, liabilityTable];
            }
            
        }
        public DataTable GITRawSheetDataFromFAGLL(DataTable filteredAdvanceGLData, DataTable filteredLiabilityGLData, bool withprofitCenter=false, bool forSnaData=false)
        {

            if (forSnaData)
            {
                DataTable PivotedLiabilityGLData = _githelper.PivotFAGLL03SNAData(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _githelper.PerformCascadedJoinSNAData(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;
            }

            else if (withprofitCenter && !forSnaData)
            {
                DataTable PivotedLiabilityGLData = _githelper.PivotLiabilityGLDataWithProfitCenter(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _githelper.PerformCascadedJoinWithProfitCenter(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;
            }

            else 
            {
                DataTable PivotedLiabilityGLData = _githelper.PivotLiabilityGLDataWithoutProfitCenter(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _githelper.PerformCascadedJoinWithoutProfitCenter(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;

            }

        }
        public DataTable AppendTradeAndGITLiabilityTable(DataTable populatedData, DataTable LiabilityData)
        {
            DataTable resultsTable = _githelper.LineItemWiseAdvanceAdjustment(populatedData, LiabilityData);
            return resultsTable;
        }
    }
}

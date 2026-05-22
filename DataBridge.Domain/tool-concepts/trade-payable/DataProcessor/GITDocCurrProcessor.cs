using System.Data;


namespace Application.DataProcessor
{
    public  class GITDocCurrProcessor(HelperFunctions helper, GITDocCurrHelper gitdochelper)
    {
        private readonly HelperFunctions _helper = helper;
        private readonly GITDocCurrHelper _gitdochelper = gitdochelper;
        public List<DataTable> ProcessFAGLL03DocCurrGITData(DataTable data)
        {
            DataTable dataCopy = HelperFunctions.DeepCopyDataTable(data, "A copy of Populated Data");

            DataTable DataWithPO = GITHelper.FilterForDataWithPO(dataCopy);
            DataTable DataWithoutPO = GITHelper.FilterForDataWithoutPO(dataCopy);
            DataTable SNAData = GITHelper.FilterForSNAData(dataCopy);

            List<DataTable> DataGroupedWithoutProfitCenter = GITDocCurrHelper.GroupFAGLL03DocCurrDataWithoutProfitCenter(DataWithPO); 
            List<DataTable> DataGroupedWithProfitCenter = GITDocCurrHelper.GroupFAGLL03DocCurrDataWithProfitCenter(DataWithoutPO);
            List<DataTable> GroupedSNAData = GITDocCurrHelper.GroupFAGLL03DocCurrDataForSNA(SNAData);

            

            DataTable allgrouped = HelperFunctions.AppendTablesWithSameColumnNames(DataGroupedWithoutProfitCenter[1], DataGroupedWithProfitCenter[1], "PO,NonPO Grouped Data");
            DataTable AllGLGroupedData = HelperFunctions.AppendTablesWithSameColumnNames(allgrouped, GroupedSNAData[1], "All Grouped Data");


            // Modified original Table With Grouping Keys
            DataTable modifiedoriginal = HelperFunctions.AppendTablesWithSameColumnNames(DataGroupedWithoutProfitCenter[0], GroupedSNAData[0], "Original data with grouping keys");
            DataTable ModifiedOriginalTable = HelperFunctions.AppendTablesWithSameColumnNames(modifiedoriginal, DataGroupedWithProfitCenter[0], "Original data with grouping keys");

            //var GITWithoutPC = FilterAndProcessGroupedGITDocCurrData(DataGroupedWithoutProfitCenter[0], false);
            //var GITWithPC = FilterAndProcessGroupedGITDocCurrData(DataGroupedWithProfitCenter[0], true);

            var GITWithoutPC = FilterAndProcessGroupedGITDocCurrData(DataGroupedWithoutProfitCenter[1], false, false);
            var GITWithPC = FilterAndProcessGroupedGITDocCurrData(DataGroupedWithProfitCenter[1], true, false);
            var GITForSNA = FilterAndProcessGroupedGITDocCurrData(GroupedSNAData[1], false, true);

            //DataTable RawGITSheet = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[0], GITWithPC[0], "Final ICP GIT Raw Sheet");
            DataTable processedgit = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[1], GITWithPC[1], "");
            DataTable processedGITData = HelperFunctions.AppendTablesWithSameColumnNames(processedgit, GITForSNA[1], "Final ProcessedGIT Sheet");


            DataTable netliability = HelperFunctions.AppendTablesWithSameColumnNames(GITWithoutPC[2], GITWithPC[2], "");
            DataTable netLiabilityTable = HelperFunctions.AppendTablesWithSameColumnNames(netliability, GITForSNA[2], "Final Net Liability Table");


            return [processedGITData, netLiabilityTable, ModifiedOriginalTable, AllGLGroupedData];
        }
        private  DataTable GITRawSheetDocCurrDataFromFAGLL(DataTable filteredAdvanceGLData, DataTable filteredLiabilityGLData, bool withprofitCenter = false, bool forSnaData = false)
        {
            if (forSnaData)
            {
                DataTable PivotedLiabilityGLData = _gitdochelper.PivotFAGLL03DocCurrSNAData(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _gitdochelper.PerformCascadedJoinDocCurrSNAData(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;
            }
            else if (withprofitCenter && !forSnaData)
            {
                DataTable PivotedLiabilityGLData = _gitdochelper.PivotLiabilityGLDocCurrDataWithProfitCenter(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _gitdochelper.PerformDocCurrCascadedJoinWithProfitCenter(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;

            }
            else
            {
                DataTable PivotedLiabilityGLData = _gitdochelper.PivotLiabilityGLDocCurrDataWithoutProfitCenter(filteredLiabilityGLData);
                DataTable JoinedAdvanceAndLiabilityData = _gitdochelper.PerformDocCurrCascadedJoinWithoutProfitCenter(filteredAdvanceGLData, PivotedLiabilityGLData);
                return JoinedAdvanceAndLiabilityData;
            }

        }




        private  List<DataTable> FilterAndProcessGroupedGITDocCurrData(DataTable data, bool withProfitCenter = false, bool forSnaData = false)
        {
            List<string> LiabilityGLs = [.. _helper.LiabilityGLs.Select(gl => gl.GL_Code)];

            if (forSnaData)
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);
                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);
                DataTable RawGITSheet = GITRawSheetDocCurrDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, false, true);
                DataTable proccessedGITData = GITDocCurrHelper.GITAdvanceManipulationFAGLL03DocCurrData(RawGITSheet, true);
                DataTable liabilityTable = GITDocCurrHelper.UnpivotProcessedGIT2(proccessedGITData);
                return [RawGITSheet, proccessedGITData, liabilityTable];
            }

            else if (withProfitCenter  && !forSnaData)
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);
                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);
                DataTable RawGITSheet = GITRawSheetDocCurrDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, true, false);
                DataTable proccessedGITData = GITDocCurrHelper.GITAdvanceManipulationFAGLL03DocCurrData(RawGITSheet);
                DataTable liabilityTable = GITDocCurrHelper.UnpivotProcessedGIT2(proccessedGITData);
                return [RawGITSheet, proccessedGITData, liabilityTable];
            }

            else
            {
                DataTable filteredAdvanceGLData = GITHelper.FilterForAdvanceGLs(data);

                DataTable filteredLiabilityGLData = GITHelper.FilterForLiabilityGLs(data, LiabilityGLs);

                DataTable RawGITSheet = GITRawSheetDocCurrDataFromFAGLL(filteredAdvanceGLData, filteredLiabilityGLData, false, false);

                DataTable proccessedGITData = GITDocCurrHelper.GITAdvanceManipulationFAGLL03DocCurrData(RawGITSheet);

                DataTable liabilityTable = GITDocCurrHelper.UnpivotProcessedGIT2(proccessedGITData);

                return [RawGITSheet, proccessedGITData, liabilityTable];
            }





        }
        public  DataTable AppendTradeAndGITLiabilityDocCurrTable(DataTable populatedData, DataTable LiabilityData)
        {
            DataTable resultsTable = _gitdochelper.DocCurrLineItemWiseAdvanceAdjustment(populatedData, LiabilityData);
            return resultsTable;
        }
    }
}

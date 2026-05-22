using Application.Services.MasterTableServices;
using Domain.Aggregates;
using Domain.Aggregates.Static_Master_Tables;
using Domain.Models.UserInputs;
using System.Data;


namespace Application.DataProcessor
{
    public class HelperFunctions
    {
        public readonly StaticMasterTableService _mastertableservice;

        public  IEnumerable<AdvanceGLs> AdvanceGLs = null!;
        public  IEnumerable<LiabilityGLs> LiabilityGLs = null!;
        public  IEnumerable<NotDueGLs> NotDueGL = null!;
        public  IEnumerable<MSMECompanyCodes> MSMECompanyCodes = null!;
        public  IEnumerable<CapitalCreditorGLs> CapitalGLs = null!;
        public  IEnumerable<UnclaimedGLs> UnclaimedGLs = null!;
        public  IEnumerable<InsuranceGLs> InsurerGLs = null!;
        public  IEnumerable<NonMSMEGLs> NonMSMSEGLs = null!;
        public  IEnumerable<AgeingGroup> AgeingGroup = null!;
        public  IEnumerable<ICPVendorMap> ICPVendorMaps = null!;
        public  IEnumerable<ICPHyperionMap> ICPHyperionMaps = null!;
        public  IEnumerable<ForexMonthEndMap> ForexMonthEndMaps = null!;



        public HelperFunctions (StaticMasterTableService mastertableserice) {
            _mastertableservice = mastertableserice;
            FillData();
        }
        async public void FillData()
        {
            AdvanceGLs = await _mastertableservice.GetAdvanceGLs();
            LiabilityGLs = await _mastertableservice.GetLiabilityGLs();
            NotDueGL = await _mastertableservice.GetNotDueGLs();
            MSMECompanyCodes = await _mastertableservice.GetMSMECCData();
            CapitalGLs = await _mastertableservice.GetCapitalCreditorGLs();
            UnclaimedGLs = await _mastertableservice.GetUnclaimedGls();
            InsurerGLs = await _mastertableservice.GetInsuranceGLs();
            NonMSMSEGLs = await _mastertableservice.GetNonMSMEGls();
            ICPVendorMaps = await _mastertableservice.GetICPVendorMap();

            AgeingGroup = await _mastertableservice.GetAgeingGroups();
            ICPHyperionMaps = await _mastertableservice.GetICPHyperionMap();
            ForexMonthEndMaps = await _mastertableservice.GetForexMonthEndMap();
        }


        public bool ISAdvanceGL(DataRow row)
        {
            if (row.IsNull("GL_Account"))
                throw new ArgumentNullException(nameof(row), "GL Account is null!");

            var glAccount = row["GL_Account"].ToString();
            return AdvanceGLs.Any(advanceGl => advanceGl.GL_Code.Equals(glAccount));
        }
        public  bool ISMSMED(DataRow Row)
        {
            if (ISAdvanceGL(Row))
                return false;

            var row_cc = Row["Company_Code"].ToString();
            var row_gl = Row["GL_Account"].ToString();
            var row_ind = Row["Industry"].ToString();

            bool isIndustryMatch = row_ind == "1" || row_ind == "2" || row_ind == "3";

            return  isIndustryMatch && 
                MSMECompanyCodes.Any(code => code.Company_Code == row_cc) && 
                !UnclaimedGLs.Any(ucgl => ucgl.Gl_Code == row_gl) && 
                !NonMSMSEGLs.Any(nmgl => nmgl.Gl_Code == row_gl);
        }
        public  bool ISCapitalRevenue(DataRow row)
        {
            var row_gl = row["GL_Account"].ToString();
            var row_po = row["Purchasing_Document"].ToString();

            if(string.IsNullOrEmpty(row_gl) || string.IsNullOrEmpty(row_po)) return false;
            else if (ISAdvanceGL(row)) return false;
            else if (ISMSMED(row)) return false;
            else return CapitalGLs.Any(cgl=>cgl.Gl_Code == row_gl) && 
                    row_po.StartsWith("71");
            

        }
        public bool IsAdvanceGL(FAGLL03ProcessedResult entity)
        {
            var glAccount = entity.GL_Account;

            if (string.IsNullOrEmpty(glAccount))
                throw new ArgumentNullException(nameof(entity), "GL Account is null or empty!");

            return AdvanceGLs.Any(advanceGl => advanceGl.GL_Code.Equals(glAccount));
        }
        public bool IsMSMED(FAGLL03ProcessedResult entity)
        {
            if (IsAdvanceGL(entity))
                return false;

            var row_cc = entity.Company_Code;
            var row_gl = entity.GL_Account;
            var row_ind = entity.Industry;

            // Check if Industry is "1", "2", or "3"
            bool isIndustryMatch = row_ind == "1" || row_ind == "2" || row_ind == "3";

            return isIndustryMatch &&
                MSMECompanyCodes.Any(code => code.Company_Code == row_cc) &&
                !UnclaimedGLs.Any(ucgl => ucgl.Gl_Code == row_gl) &&
                !NonMSMSEGLs.Any(nmgl => nmgl.Gl_Code == row_gl);
        }
        public bool IsCapitalRevenue(FAGLL03ProcessedResult entity)
        {
            var row_gl = entity.GL_Account;
            var row_po = entity.Purchasing_Document;

            // If GL or PO is missing, it cannot be Capital Revenue
            if (string.IsNullOrEmpty(row_gl) || string.IsNullOrEmpty(row_po)) return false;

            if (IsAdvanceGL(entity)) return false;
            if (IsMSMED(entity)) return false;

            return CapitalGLs.Any(cgl => cgl.Gl_Code == row_gl) &&
                row_po.StartsWith("71");
        }
        public static IEnumerable<FAGLL03ProcessedResult> CalculateJournalEntryEnumerable (IEnumerable<FAGLL03ProcessedResult> data)
        {
            foreach (var entity in data)
            {
                decimal net_amount = entity.Amount_Local_Adjusted;
                var base_hyperion = entity.Base_Hyperion_Code;
                var dest_hyperion = entity.Hyperion_Code;
                if (base_hyperion == dest_hyperion)
                {
                    entity.Base_Hyperion_Debit = 0m;
                    entity.Destination_Hyperion_Credit = 0m;
                }
                else
                {
                    entity.Base_Hyperion_Debit = Math.Abs(net_amount);
                    entity.Destination_Hyperion_Credit = Math.Abs(net_amount);
                }
                yield return entity;
            }
        }
        public static decimal GetDecimalValue(DataRow row, string columnName)
        {
            // The IsNull() method is the safest way to check for DBNull.Value.
            if (row.IsNull(columnName))
            {
                return 0;
            }

            var cellValue = row[columnName].ToString();
            if (string.Equals(cellValue, "NULL", StringComparison.OrdinalIgnoreCase))
                return 0;

            _ = decimal.TryParse(cellValue, out decimal parsedValue);
            return parsedValue;
        }
        public static DataTable DeepCopyDataTable(DataTable originalTable, string tableName)
        {
            //Create a new DataTable with the same schema
            DataTable newTable = new DataTable(tableName);
            foreach (DataColumn column in originalTable.Columns)
            {
                newTable.Columns.Add(column.ColumnName, column.DataType);
            }
            //Import the rows, which creates new DataRow objects
            foreach (DataRow row in originalTable.Rows)
            {
                newTable.ImportRow(row);
            }
            return newTable;
        }
        public static DataTable AppendTablesWithSameColumnNames(DataTable table1, DataTable table2, string finalTableName)
        {
            DataTable resultsTable = DeepCopyDataTable(table1, finalTableName);

            foreach (DataRow row in table2.Rows)
            {
                resultsTable.ImportRow(row);
            }
            return resultsTable;
        }
        public static DataTable AssignCorporateLabel(DataTable Data)
        {
            var resultsTable = DeepCopyDataTable(Data, "Data with Corporate Label");
            resultsTable.Columns.Add("Vertical", typeof(string));

            foreach (DataRow row in resultsTable.Rows)
            {
                var cc = row["Company_Code"].ToString();
                string c_type;
                if (cc == "1000")
                    c_type = "Corporate";
                else c_type = "Offshore";

                row["Vertical"] = c_type;
            }

            return resultsTable;
        }

        public static DateTime GetCurrentQuarterDetails(ProcessStartInitialInputs inputs)
        {
            int year;
            int month;
            int day;

            if (inputs.QuarterYear.Length > 4)
                year = 2025;
            else year = int.Parse(inputs.QuarterYear);

            if (inputs.QuarterMonth.StartsWith("0") && inputs.QuarterMonth.Length == 2)
                month = int.Parse(inputs.QuarterMonth.Substring(1));
            else if (inputs.QuarterMonth.StartsWith("0") && inputs.QuarterMonth.Length == 3)
                month = int.Parse(inputs.QuarterMonth.Substring(2));
            else month = int.Parse(inputs.QuarterMonth);
            if (month > 12)
                month = 12;

            if (inputs.QuarterDay.StartsWith("0") && inputs.QuarterDay.Length == 2)
                day = int.Parse(inputs.QuarterDay.Substring(1));
            else if (inputs.QuarterDay.StartsWith("0") && inputs.QuarterDay.Length == 3)
                day = int.Parse(inputs.QuarterDay.Substring(2));
            else day = int.Parse(inputs.QuarterDay);
            if (day >= 31)
                day = 30;


            DateTime QuarterDate = new DateTime(year, month, day);

            return QuarterDate;
        }
        public static DateTime GetLastDateOfCurrentQuarter(DateTime currentDate)
        {

            int currentMonth = currentDate.Month;
            int currentYear = currentDate.Year;

            DateTime lastDayOfQuarter;

            if (currentMonth >= 1 && currentMonth <= 3) // Q1: January - March
            {
                lastDayOfQuarter = new DateTime(currentYear, 3, 31);
            }
            else if (currentMonth >= 4 && currentMonth <= 6) // Q2: April - June
            {
                lastDayOfQuarter = new DateTime(currentYear, 6, 30);
            }
            else if (currentMonth >= 7 && currentMonth <= 9) // Q3: July - September
            {
                lastDayOfQuarter = new DateTime(currentYear, 9, 30);
            }
            else // Q4: October - December
            {
                lastDayOfQuarter = new DateTime(currentYear, 12, 31);
            }

            return lastDayOfQuarter;
        }
        public static DateTime GetLastDayOfPreviousQuarter(DateTime inputDate)
        {
            int currentQuarterStartMonth = (inputDate.Month - 1) / 3 * 3 + 1;
            DateTime currentQuarterStart = new(inputDate.Year, currentQuarterStartMonth, 1);
            DateTime lastDayOfPreviousQuarter = currentQuarterStart.AddDays(-1);
            return lastDayOfPreviousQuarter;
        }
        public static DateTime GetLastDayOfQuarter(DateTime inputDate)
        {
            int currentQuarterStartMonth = (inputDate.Month - 1) / 3 * 3 + 1;
            DateTime currentQuarterStart = new(inputDate.Year, currentQuarterStartMonth, 1);
            return currentQuarterStart.AddMonths(3).AddDays(-1);
        }
    }
}

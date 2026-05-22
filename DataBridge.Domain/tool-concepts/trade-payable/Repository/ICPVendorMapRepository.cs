using Dapper;
using Domain.Aggregates;
using Domain.Aggregates.Static_Master_Tables;
using Domain.Shared;
using Infrastructure.Contracts;
using Infrastructure.Dapper;


namespace Infrastructure.Repository
{
    public class ICPVendorMapRepository(DapperContext dbcontext): IICPVendorMapRepository
    {
        private readonly DapperContext _dbcontext = dbcontext;
        public async Task<IEnumerable<ICPVendorMap>> GetAllAsync()
        {
            string sql = "SELECT * FROM [dbo].[MASTER_TradePayables_ICPVendorMap]";
            using var connection = _dbcontext.CreateConnection("default"); 
            var data = await connection.QueryAsync<ICPVendorMap>(sql);
            return data;
        }
        public async Task<IEnumerable<string>> GetAllEntityRelations()
        {
            string sql = @"
                    SELECT DISTINCT [Entity_Relation]
                    FROM [dbo].[MASTER_TradePayables_ICPVendorMap]
                    WHERE [Entity_Relation] IS NOT NULL 
                      AND [Entity_Relation] <> 'NULL'
                      AND [Entity_Relation] <> ''";
            using var connection = _dbcontext.CreateConnection("default");

            var data = await connection.QueryAsync<string>(sql);
            return data.Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public async Task<IEnumerable<string>> GetAllEntityTypes()
        {
            string sql = @"
                    SELECT DISTINCT [Entity_Type]
                    FROM [dbo].[MASTER_TradePayables_ICPVendorMap]
                    WHERE [Entity_Type] IS NOT NULL 
                      AND [Entity_Type] <> 'NULL'
                      AND [Entity_Type] <> '' ";
            using var connection = _dbcontext.CreateConnection("default");
            var data = await connection.QueryAsync<string>(sql);
            return data.Where(x => !string.IsNullOrWhiteSpace(x));
        }
        public async Task<ICPVendorMap?> GetByVendorCodeAsync(string vendor_code)
        {
            string sql = "SELECT TOP (1) * FROM [dbo].[MASTER_TradePayables_ICPVendorMap] WHERE Vendor_Code = @Vendor_Code";
            using var connection = _dbcontext.CreateConnection("default");
            return await connection.QueryFirstOrDefaultAsync<ICPVendorMap>(sql, new { Vendor_Code = vendor_code });
        }
        public async Task<Message> UpdateByVendorAsync(ICPVendorMap model)
        {
            var msg = new Message { Title = "Update Vendor Mapping" };

            // 1. Validation Logic
            var validationErrors = GetValidationErrors(model);
            if (validationErrors.Any())
            {
                msg.Success = false;
                msg.Text = $"Validation Failed: {string.Join(", ", validationErrors)}";
                return msg;
            }

            string updateSql = @"
        UPDATE [dbo].[MASTER_TradePayables_ICPVendorMap]
        SET 
            Vendor_Name = @Vendor_Name,
            ICP_Name = @ICP_Name,
            Entity_Type = @Entity_Type,
            Entity_Relation = @Entity_Relation,
            Approver_PSNO = @Approver_PSNO,
            Approver_Name = @Approver_Name,
            IsActive = @IsActive
        WHERE Vendor_Code = @Vendor_Code";

            using var connection = _dbcontext.CreateConnection("default");

            try
            {
                int rowsAffected = await connection.ExecuteAsync(updateSql, model);

                if (rowsAffected > 0)
                {
                    msg.Success = true;
                    msg.Text = "Vendor details updated successfully.";
                }
                else
                {
                    msg.Success = false;
                    msg.Text = "Update failed: Vendor Code not found.";
                }
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Database Error: {ex.Message}";
            }

            return msg;
        }
        public async Task<Message> InsertVendorMapAsync(ICPVendorMap model)
        {
            var msg = new Message { Title = "Insert Vendor Mapping" };

            // 1. Validation Logic
            var validationErrors = GetValidationErrors(model);
            if (validationErrors.Count != 0)
            {
                msg.Success = false;
                msg.Text = $"Validation Failed: {string.Join(", ", validationErrors)}";
                return msg;
            }

            using var connection = _dbcontext.CreateConnection("default");

            try
            {
                // 2. Duplicate Check
                string checkSql = "SELECT COUNT(1) FROM [dbo].[MASTER_TradePayables_ICPVendorMap] WHERE Vendor_Code = @Vendor_Code";
                var exists = await connection.ExecuteScalarAsync<bool>(checkSql, new { model.Vendor_Code });

                if (exists)
                {
                    msg.Success = false;
                    msg.Text = $"The Vendor Code '{model.Vendor_Code}' already exists in the system.";
                    return msg;
                }

                // 3. Insert Execution
                string insertSql = @"
                    INSERT INTO [dbo].[MASTER_TradePayables_ICPVendorMap]
                    (Vendor_Code, Vendor_Name, ICP_Name, Entity_Type, Entity_Relation, Approver_PSNO, Approver_Name, IsActive)
                    VALUES 
                    (@Vendor_Code, @Vendor_Name, @ICP_Name, @Entity_Type, @Entity_Relation, @Approver_PSNO, @Approver_Name, @IsActive)";

                await connection.ExecuteAsync(insertSql, model);

                msg.Success = true;
                msg.Text = "Vendor mapping created successfully.";
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Database Error: {ex.Message}";
            }

            return msg;
        }
        /// <summary>
        /// Business Use Case: Change the active status of a vendor mapping.
        /// </summary>
        /// <param name="vendor_code">The unique vendor identifier.</param>
        /// <param name="status">True to activate, False to deactivate.</param>
        public async Task<Message> ToggleVendorMapIsActiveStatus(string vendor_code, bool status)
        {
            // Dynamic title based on intent
            string action = status ? "Activate" : "Deactivate";
            var msg = new Message { Title = $"{action} Vendor Mapping" };

            if (string.IsNullOrWhiteSpace(vendor_code))
            {
                msg.Success = false;
                msg.Text = "Vendor Code is required.";
                return msg;
            }

            // SQL updates IsActive to 1 (true) or 0 (false)
            string sql = @"
                    UPDATE [dbo].[MASTER_TradePayables_ICPVendorMap]
                    SET IsActive = @Status 
                    WHERE Vendor_Code = @Vendor_Code";

            using var connection = _dbcontext.CreateConnection("default");

            try
            {
                int rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    Vendor_Code = vendor_code,
                    Status = status
                });

                if (rowsAffected > 0)
                {
                    msg.Success = true;
                    msg.Text = $"Vendor '{vendor_code}' has been successfully {action.ToLower()}d.";
                }
                else
                {
                    msg.Success = false;
                    msg.Text = $"Record with Vendor Code '{vendor_code}' not found.";
                }
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Database Error: {ex.Message}";
            }

            return msg;
        }



        // helpers
        private static List<string> GetValidationErrors(ICPVendorMap model)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Vendor_Code)) errors.Add("Vendor Code is required");
            if (string.IsNullOrWhiteSpace(model.Vendor_Name)) errors.Add("Vendor Name is required");
            if (string.IsNullOrWhiteSpace(model.ICP_Name)) errors.Add("ICP Name is required");
            //if (string.IsNullOrWhiteSpace(model.Approver_PSNO)) errors.Add("Approver PSNO is required");
            return errors;
        }
    }
}

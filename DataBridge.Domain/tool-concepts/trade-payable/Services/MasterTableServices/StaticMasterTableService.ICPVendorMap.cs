using Domain.Aggregates.Static_Master_Tables;
using Domain.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.MasterTableServices
{
    
        public partial class StaticMasterTableService
        {
        // ICP Vendor Map Service Methods //

        /// <summary>
        /// Business Use Case: Fetch all ICP Vendor Map data for presentation.
        /// </summary>
        public async Task<IEnumerable<ICPVendorMap>> GetICPVendorMap()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all ICPVendorMap data.");
            var data = await _icpvendormaprepo.GetAllAsync();
            return data;
        }
        public async Task<ICPVendorMap?> GetICPVendorMapByVendorCode(string vendorCode)
        {
            return await _icpvendormaprepo.GetByVendorCodeAsync(vendorCode);
        }
        public async Task<IEnumerable<string>> GetAllEntityRelations()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all ICPVendorMap data.");
            var data = await _icpvendormaprepo.GetAllEntityRelations();
            return data;
        }
        public async Task<IEnumerable<string>> GetAllEntityTypes()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all ICPVendorMap data.");
            var data = await _icpvendormaprepo.GetAllEntityTypes();
            return data;
        }


        /// <summary>
        /// Business Use Case: Create a new Vendor Mapping after validating required fields and duplicates.
        /// </summary>
        public async Task<Message> InsertICPVendorMapAsync(ICPVendorMap model)
        {
            var msg = new Message { Title = "Create Mapping" };

            var validationErrors = ValidateICPVendorUpdateRequest(model);
            if (validationErrors.Count != 0)
            {
                msg.Success = false;
                msg.Text = $"Missing Fields: {string.Join(", ", validationErrors)}";
                return msg;
            }

            try
            {
                var existing = await _icpvendormaprepo.GetByVendorCodeAsync(model.Vendor_Code);
                if (existing != null)
                {
                    msg.Success = false;
                    msg.Text = $"Vendor Code '{model.Vendor_Code}' is already mapped. Please use Edit instead.";
                    return msg;
                }
                await _icpvendormaprepo.InsertVendorMapAsync(model);
                msg.Success = true;
                msg.Text = "New ICP Vendor mapping successfully created.";
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Error during creation: {ex.Message}";
            }
            return msg;
        }

        /// <summary>
        /// Business Use Case: Update existing mapping details.
        /// </summary>
        public async Task<Message> UpdateICPVendorMapAsync(ICPVendorMap model)
        {
            var msg = new Message { Title = "Update Mapping" };

            var validationErrors = ValidateICPVendorUpdateRequest(model);
            if (validationErrors.Count != 0)
            {
                msg.Success = false;
                msg.Text = $"Validation Error: {string.Join(", ", validationErrors)}";
                return msg;
            }

            try
            {
                var updated = await _icpvendormaprepo.UpdateByVendorAsync(model);

                if (updated.Success)
                {
                    msg.Success = true;
                    msg.Text = $"Vendor {model.Vendor_Code} updated successfully.";
                }
                else
                {
                    msg.Success = false;
                    msg.Text = "No record found to update. It may have been removed by another user.";
                }
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Update failed: {ex.Message}";
            }

            return msg;
        }

        /// <summary>
        /// Business Use Case: Remove a vendor mapping entry.
        /// </summary>
        public async Task<Message> ToggleVendorMapStatusAsync(string vendorCode, bool status)
        {
            // Keeping Title as 'Delete' for the UI, but the underlying action is deactivation
            var msg = new Message { Title = "Toggle Mapping Status" };

            if (string.IsNullOrWhiteSpace(vendorCode))
            {
                msg.Success = false;
                msg.Text = "A valid Vendor Code must be provided.";
                return msg;
            }

            try
            {
                // Calling the toggle method with status 'false' to perform soft delete
                // We capture the message returned directly from the repository
                msg = await _icpvendormaprepo.ToggleVendorMapIsActiveStatus(vendorCode, status);

                // Customizing the text if the repo operation was successful
                if (msg.Success)
                {
                    msg.Text = $"Vendor mapping for {vendorCode} has been marked as {status}.";
                }
            }
            catch (Exception ex)
            {
                msg.Success = false;
                msg.Text = $"Critical error during deactivation: {ex.Message}";
            }

            return msg;
        }

        /// <summary>
        /// Private helper to ensure data integrity before DB interaction.
        /// </summary>
        private static List<string> ValidateICPVendorUpdateRequest(ICPVendorMap model)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Vendor_Code)) errors.Add("Vendor Code");
            if (string.IsNullOrWhiteSpace(model.Vendor_Name)) errors.Add("Vendor Name");
            if (string.IsNullOrWhiteSpace(model.ICP_Name)) errors.Add("ICP Name");
            //if (string.IsNullOrWhiteSpace(model.Approver_PSNO)) errors.Add("Approver PSNO");

            return errors;
        }
    }
}

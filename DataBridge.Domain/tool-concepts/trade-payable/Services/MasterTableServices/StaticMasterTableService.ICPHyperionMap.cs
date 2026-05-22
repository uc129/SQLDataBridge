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

        // ICP Hyperion Map Service Methods //

        public async Task<IEnumerable<ICPHyperionMap>> GetICPHyperionMap()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all ICPHyperionMap data.");
            var data = await _icphyperionmaprepo.GetAllAsync();
            return data;
        }

        //public async Task<Message> InsertICPHyperionMapAsync(ICPHyperionMap model)
        //{
        //    var msg = new Message { Title = "Create Mapping" };

        //    var validationErrors = ValidateICPHyperionUpdateRequest(model);
        //    if (validationErrors.Count != 0)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Missing Fields: {string.Join(", ", validationErrors)}";
        //        return msg;
        //    }

        //    try
        //    {
        //        var existing = await _icphyperionmaprepo.GetByHyperionCodeAsync(model.Hyperion_Code);
        //        if (existing != null)
        //        {
        //            msg.Success = false;
        //            msg.Text = $"Hyperion Code '{model.Hyperion_Code}' is already mapped. Please use Edit instead.";
        //            return msg;
        //        }
        //        await _icphyperionmaprepo.InsertHyperionMapAsync(model);
        //        msg.Success = true;
        //        msg.Text = "New ICP Hyperion mapping successfully created.";
        //    }
        //    catch (Exception ex)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Error during creation: {ex.Message}";
        //    }
        //    return msg;
        //}

        //public async Task<Message> UpdateICPHyperionMapAsync(ICPHyperionMap model)
        //{
        //    var msg = new Message { Title = "Update Mapping" };

        //    var validationErrors = ValidateICPHyperionUpdateRequest(model);
        //    if (validationErrors.Count != 0)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Validation Error: {string.Join(", ", validationErrors)}";
        //        return msg;
        //    }

        //    try
        //    {
        //        var updated = await _icphyperionmaprepo.UpdateByHyperionAsync(model);

        //        if (updated.Success)
        //        {
        //            msg.Success = true;
        //            msg.Text = $"Hyperion {model.Hyperion_Code} updated successfully.";
        //        }
        //        else
        //        {
        //            msg.Success = false;
        //            msg.Text = "No record found to update. It may have been removed by another user.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Update failed: {ex.Message}";
        //    }

        //    return msg;
        //}

        //public async Task<Message> ToggleHyperionMapStatusAsync(string vendorCode, bool status)
        //{
        //    // Keeping Title as 'Delete' for the UI, but the underlying action is deactivation
        //    var msg = new Message { Title = "Toggle Mapping Status" };

        //    if (string.IsNullOrWhiteSpace(vendorCode))
        //    {
        //        msg.Success = false;
        //        msg.Text = "A valid Hyperion Code must be provided.";
        //        return msg;
        //    }

        //    try
        //    {
        //        // Calling the toggle method with status 'false' to perform soft delete
        //        // We capture the message returned directly from the repository
        //        msg = await _icphyperionmaprepo.ToggleHyperionMapIsActiveStatus(vendorCode, status);

        //        // Customizing the text if the repo operation was successful
        //        if (msg.Success)
        //        {
        //            msg.Text = $"Hyperion mapping for {vendorCode} has been marked as {status}.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Critical error during deactivation: {ex.Message}";
        //    }

        //    return msg;
        //}

        private static List<string> ValidateICPHyperionUpdateRequest(ICPHyperionMap model)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Hyperion_Credit)) errors.Add("Hyperion Credit is null");
            if (string.IsNullOrWhiteSpace(model.Hyperion_Debit)) errors.Add("Hyperion Debit is null");
            if (string.IsNullOrWhiteSpace(model.ICP_Name)) errors.Add("ICP Name is null");
            return errors;
        }
    }
}

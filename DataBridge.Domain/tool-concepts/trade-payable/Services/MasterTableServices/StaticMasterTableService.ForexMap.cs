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
        // ICP Forex Map Service Methods //

        public async Task<IEnumerable<ForexMonthEndMap>> GetForexMonthEndMap()
        {
            System.Diagnostics.Debug.WriteLine("Fetching all ForexMap data.");
            var data = await _forexmonthendmaprepo.GetAllAsync();
            return data;
        }

        //public async Task<Message> InsertForexMapAsync(ForexMonthEndMap model)
        //{
        //    var msg = new Message { Title = "Create Mapping" };

        //    var validationErrors = ValidateForexUpdateRequest(model);
        //    if (validationErrors.Count != 0)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Missing Fields: {string.Join(", ", validationErrors)}";
        //        return msg;
        //    }

        //    try
        //    {
        //        var existing = await _forexmonthendmaprepo.GetByForexCodeAsync(model.Forex_Code);
        //        if (existing != null)
        //        {
        //            msg.Success = false;
        //            msg.Text = $"Forex Code '{model.Forex_Code}' is already mapped. Please use Edit instead.";
        //            return msg;
        //        }
        //        await _forexmonthendmaprepo.InsertForexMapAsync(model);
        //        msg.Success = true;
        //        msg.Text = "New ICP Forex mapping successfully created.";
        //    }
        //    catch (Exception ex)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Error during creation: {ex.Message}";
        //    }
        //    return msg;
        //}

        //public async Task<Message> UpdateForexMapAsync(ForexMonthEndMap model)
        //{
        //    var msg = new Message { Title = "Update Mapping" };

        //    var validationErrors = ValidateForexUpdateRequest(model);
        //    if (validationErrors.Count != 0)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Validation Error: {string.Join(", ", validationErrors)}";
        //        return msg;
        //    }

        //    try
        //    {
        //        var updated = await _forexmonthendmaprepo.UpdateByForexAsync(model);

        //        if (updated.Success)
        //        {
        //            msg.Success = true;
        //            msg.Text = $"Forex {model.Forex_Code} updated successfully.";
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

        //public async Task<Message> ToggleForexMapStatusAsync(string vendorCode, bool status)
        //{
        //    // Keeping Title as 'Delete' for the UI, but the underlying action is deactivation
        //    var msg = new Message { Title = "Toggle Mapping Status" };

        //    if (string.IsNullOrWhiteSpace(vendorCode))
        //    {
        //        msg.Success = false;
        //        msg.Text = "A valid Forex Code must be provided.";
        //        return msg;
        //    }

        //    try
        //    {
        //        // Calling the toggle method with status 'false' to perform soft delete
        //        // We capture the message returned directly from the repository
        //        msg = await _forexmonthendmaprepo.ToggleForexMapIsActiveStatus(vendorCode, status);

        //        // Customizing the text if the repo operation was successful
        //        if (msg.Success)
        //        {
        //            msg.Text = $"Forex mapping for {vendorCode} has been marked as {status}.";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        msg.Success = false;
        //        msg.Text = $"Critical error during deactivation: {ex.Message}";
        //    }

        //    return msg;
        //}

        private static List<string> ValidateForexUpdateRequest(ForexMonthEndMap model)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Currency)) errors.Add("Currency Error");
            if (model.Conversion_Rate <=0 ) errors.Add("Conversion rate can not be zero or negative");
            if(model.Conversion_Rate ==1 && !model.Currency.Equals("INR", StringComparison.CurrentCultureIgnoreCase)) errors.Add("Conversion rate can be 1 only for INR currency");
            if(model.Date == DateTime.MinValue) errors.Add("Date Error");

            return errors;
        }
    }
}

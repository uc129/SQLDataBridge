using DataBridge.Api.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DataBridge.Api.Controllers;

[ApiController]
[Route("api/v1/connection")]
[Authorize]
public class ConnectionController(IConfiguration config) : ControllerBase
{
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionHttpRequest req)
    {
        var cs = string.IsNullOrWhiteSpace(req.ConnectionString)
            ? config.GetConnectionString("Default") ?? string.Empty
            : req.ConnectionString;

        if (string.IsNullOrWhiteSpace(cs))
            return Ok(new { success = false, message = "No connection string provided or configured." });

        try
        {
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();
            return Ok(new { success = true, message = $"Connected to {conn.DataSource}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

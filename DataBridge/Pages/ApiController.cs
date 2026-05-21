using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DataBridge.Pages;

[ApiController]
[Authorize]
[Route("api")]
public class ApiController(IConfiguration config) : ControllerBase
{
    // Tests a caller-supplied connection string
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ConnectionString))
        {
            // Fall back to the server-configured default
            req.ConnectionString = config.GetConnectionString("Default") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(req.ConnectionString))
            return Ok(new { success = false, message = "No connection string provided or configured." });

        try
        {
            await using var conn = new SqlConnection(req.ConnectionString);
            await conn.OpenAsync();
            return Ok(new { success = true, message = $"Connected to {conn.DataSource}" });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class TestConnectionRequest
{
    public string ConnectionString { get; set; } = string.Empty;
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DataBridge.Pages;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest req)
    {
        try
        {
            await using var conn = new SqlConnection(req.ConnectionString);
            await conn.OpenAsync();
            return Ok(new { success = true, message = "Connected successfully." });
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

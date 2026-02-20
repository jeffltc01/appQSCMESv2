using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/tablet-setup")]
public class TabletSetupController : ControllerBase
{
    [HttpPost]
    public ActionResult Save([FromBody] TabletSetupDto request)
    {
        // Persist to device/local storage is typically client-side; API can validate or store user preference server-side later.
        return NoContent();
    }
}

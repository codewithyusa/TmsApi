using Microsoft.AspNetCore.Mvc;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    [HttpGet("test")]
    public IActionResult TestSaltUniqueness()
    {
        var service = new CryptoDemoService();

        string hash1 = service.HashUserPassword("Password123!");
        string hash2 = service.HashUserPassword("Password123!");

        // Both hashes should be different because BCrypt generates
        // a unique random salt for each password hash.
        bool match1 = service.VerifyUserPassword("Password123!", hash1);
        bool match2 = service.VerifyUserPassword("Password123!", hash2);

        return Ok(new
        {
            Hash1 = hash1,
            Hash2 = hash2,
            HashesAreDifferent = hash1 != hash2,
            Match1 = match1,
            Match2 = match2
        });
    }
}

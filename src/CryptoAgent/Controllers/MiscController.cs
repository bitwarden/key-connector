using Bit.CryptoAgent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Controllers
{
    public class MiscController : Controller
    {
        private readonly IRsaKeyService _rsaKeyService;

        public MiscController(
            IRsaKeyService rsaKeyService)
        {
            _rsaKeyService = rsaKeyService;
        }

        [HttpGet("~/alive")]
        [HttpGet("~/now")]
        [AllowAnonymous]
        public DateTime GetAlive()
        {
            return DateTime.UtcNow;
        }

        [HttpGet("~/public-key")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicKey()
        {
            var key = await _rsaKeyService.GetPublicKeyAsync();
            return new OkObjectResult(new { PublicKey = Convert.ToBase64String(key) });
        }
    }
}

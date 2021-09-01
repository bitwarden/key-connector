using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Bit.CryptoAgent.Controllers
{
    public class MiscController : Controller
    {
        [HttpGet("~/alive")]
        [HttpGet("~/now")]
        [AllowAnonymous]
        public DateTime GetAlive()
        {
            return DateTime.UtcNow;
        }
    }
}

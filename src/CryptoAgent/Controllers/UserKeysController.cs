using Bit.CryptoAgent.Models;
using Bit.CryptoAgent.Repositories;
using Bit.CryptoAgent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Controllers
{
    [Authorize("Application")]
    [Route("user-keys")]
    public class UserKeysController : Controller
    {
        private readonly ILogger<UserKeysController> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IUserKeyRepository _userKeyRepository;
        private readonly IdentityOptions _identityOptions;

        public UserKeysController(
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<UserKeysController> logger,
            IUserKeyRepository userKeyRepository,
            ICryptoService cryptoService)
        {
            _identityOptions = optionsAccessor?.Value ?? new IdentityOptions();
            _logger = logger;
            _cryptoService = cryptoService;
            _userKeyRepository = userKeyRepository;
        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] UserKeyGetRequestModel model)
        {
            var userId = GetProperUserId().Value;
            var publicKey = Convert.FromBase64String(model.PublicKey);
            var user = await _userKeyRepository.ReadAsync(userId);
            if (user == null)
            {
                return new NotFoundResult();
            }
            user.LastAccessDate = DateTime.UtcNow;
            await _userKeyRepository.UpdateAsync(user);
            var key = await _cryptoService.AesDecryptAsync(user.Key);
            var encKey = await _cryptoService.RsaEncryptAsync(key, publicKey);
            var response = new UserKeyResponseModel
            {
                Key = Convert.ToBase64String(encKey)
            };
            return new JsonResult(response);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] UserKeyRequestModel model)
        {
            var userId = GetProperUserId().Value;
            var user = await _userKeyRepository.ReadAsync(userId);
            if (user != null)
            {
                return new BadRequestResult();
            }
            var key = await _cryptoService.RsaDecryptAsync(Convert.FromBase64String(model.Key));
            user = new UserKeyModel
            {
                Id = userId,
                Key = await _cryptoService.AesEncryptToB64Async(key)
            };
            await _userKeyRepository.CreateAsync(user);
            return new OkResult();
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UserKeyRequestModel model)
        {
            var userId = GetProperUserId().Value;
            var user = await _userKeyRepository.ReadAsync(userId);
            if (user != null)
            {
                return new BadRequestResult();
            }
            var key = await _cryptoService.RsaDecryptAsync(Convert.FromBase64String(model.Key));
            user = new UserKeyModel
            {
                Id = userId,
                Key = await _cryptoService.AesEncryptToB64Async(key)
            };
            await _userKeyRepository.UpdateAsync(user);
            return new OkResult();
        }

        private Guid? GetProperUserId()
        {
            var userId = User.FindFirstValue(_identityOptions.ClaimsIdentity.UserIdClaimType);
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return null;
            }
            return userIdGuid;
        }
    }
}

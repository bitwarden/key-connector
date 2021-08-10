using Bit.CryptoAgent.Models;
using Bit.CryptoAgent.Repositories;
using Bit.CryptoAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Controllers
{
    [Route("user-keys")]
    public class UserKeysController : Controller
    {
        private readonly ILogger<UserKeysController> _logger;
        private readonly ICryptoService _cryptoService;
        private readonly IUserKeyRepository _userKeyRepository;

        public UserKeysController(
            ILogger<UserKeysController> logger,
            IUserKeyRepository userKeyRepository,
            ICryptoService cryptoService)
        {
            _logger = logger;
            _cryptoService = cryptoService;
            _userKeyRepository = userKeyRepository;
        }

        [HttpPost("{userId}/get")]
        public async Task<IActionResult> Get(Guid userId, [FromBody] UserKeyGetRequestModel model)
        {
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

        [HttpPost("{userId}")]
        public async Task<IActionResult> Post(Guid userId, [FromBody] UserKeyRequestModel model)
        {
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

        [HttpPut("{userId}")]
        public async Task<IActionResult> Put(Guid userId, [FromBody] UserKeyRequestModel model)
        {
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
    }
}

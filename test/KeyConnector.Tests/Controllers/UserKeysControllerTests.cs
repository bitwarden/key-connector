using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Bit.KeyConnector.Controllers;
using Bit.KeyConnector.Models;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace KeyConnector.Tests.Controllers;

public class UserKeysControllerTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly IUserKeyRepository _userKeyRepository = Substitute.For<IUserKeyRepository>();
    private readonly ICryptoService _cryptoService = Substitute.For<ICryptoService>();
    private readonly UserKeysController _sut;

    public UserKeysControllerTests()
    {
        var identityOptions = new IdentityOptions();
        var logger = Substitute.For<ILogger<UserKeysController>>();

        _sut = new UserKeysController(
            Options.Create(identityOptions),
            logger,
            _userKeyRepository,
            _cryptoService);

        var claims = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(identityOptions.ClaimsIdentity.UserIdClaimType, _userId.ToString())
        ]));
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claims }
        };
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _userKeyRepository.ReadAsync(_userId).Returns((UserKeyModel)null);

        var result = await _sut.Get();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_ReturnsDecryptedKey_WhenUserExists()
    {
        var user = new UserKeyModel { Id = _userId, Key = "encryptedKey" };
        _userKeyRepository.ReadAsync(_userId).Returns(user);
        _cryptoService.AesDecryptToB64Async("encryptedKey").Returns("decryptedKey");

        var result = await _sut.Get();

        var jsonResult = Assert.IsType<JsonResult>(result);
        var response = Assert.IsType<UserKeyResponseModel>(jsonResult.Value);
        Assert.Equal("decryptedKey", response.Key);
    }

    [Fact]
    public async Task Get_UpdatesLastAccessDate_WhenUserExists()
    {
        var user = new UserKeyModel { Id = _userId, Key = "encryptedKey" };
        _userKeyRepository.ReadAsync(_userId).Returns(user);
        _cryptoService.AesDecryptToB64Async("encryptedKey").Returns("decryptedKey");
        var beforeGet = DateTime.UtcNow;

        await _sut.Get();

        Assert.NotNull(user.LastAccessDate);
        Assert.InRange(user.LastAccessDate.Value, beforeGet, DateTime.UtcNow);
        await _userKeyRepository.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenUserAlreadyExists()
    {
        var existingUser = new UserKeyModel { Id = _userId, Key = "existingKey" };
        _userKeyRepository.ReadAsync(_userId).Returns(existingUser);

        var result = await _sut.Post(new UserKeyRequestModel { Key = "newKey" });

        Assert.IsType<BadRequestResult>(result);
        await _userKeyRepository.DidNotReceive().CreateAsync(Arg.Any<UserKeyModel>());
    }

    [Fact]
    public async Task Post_CreatesUser_WhenUserDoesNotExist()
    {
        _userKeyRepository.ReadAsync(_userId).Returns((UserKeyModel)null);
        _cryptoService.AesEncryptToB64Async("plaintextKey").Returns("encryptedKey");
        UserKeyModel capturedUser = null;
        await _userKeyRepository.CreateAsync(Arg.Do<UserKeyModel>(u => capturedUser = u));
        var beforeCreate = DateTime.UtcNow;

        var result = await _sut.Post(new UserKeyRequestModel { Key = "plaintextKey" });

        Assert.IsType<OkResult>(result);
        Assert.NotNull(capturedUser);
        Assert.Equal(_userId, capturedUser.Id);
        Assert.Equal("encryptedKey", capturedUser.Key);
        Assert.InRange(capturedUser.CreationDate, beforeCreate, DateTime.UtcNow);
        await _userKeyRepository.Received(1).CreateAsync(capturedUser);
    }

    [Fact]
    public async Task Put_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _userKeyRepository.ReadAsync(_userId).Returns((UserKeyModel)null);

        var result = await _sut.Put(new UserKeyRequestModel { Key = "newKey" });

        Assert.IsType<NotFoundResult>(result);
        await _userKeyRepository.DidNotReceive().UpdateAsync(Arg.Any<UserKeyModel>());
    }

    [Fact]
    public async Task Put_UpdatesKey_WhenUserExists()
    {
        var creationDate = DateTime.UtcNow.AddDays(-1);
        var existingUser = new UserKeyModel
        {
            Id = _userId, Key = "oldEncryptedKey", CreationDate = creationDate
        };
        _userKeyRepository.ReadAsync(_userId).Returns(existingUser);
        _cryptoService.AesEncryptToB64Async("newPlaintextKey").Returns("newEncryptedKey");
        var beforeUpdate = DateTime.UtcNow;

        var result = await _sut.Put(new UserKeyRequestModel { Key = "newPlaintextKey" });

        Assert.IsType<OkResult>(result);
        Assert.Equal(_userId, existingUser.Id);
        Assert.Equal("newEncryptedKey", existingUser.Key);
        Assert.Equal(creationDate, existingUser.CreationDate);
        Assert.NotNull(existingUser.RevisionDate);
        Assert.InRange(existingUser.RevisionDate.Value, beforeUpdate, DateTime.UtcNow);
        await _userKeyRepository.Received(1).UpdateAsync(existingUser);
    }
}

using Bit.CryptoAgent.Models;
using System;

namespace Bit.CryptoAgent.Repositories
{
    public interface IUserKeyRepository : IRepository<UserKeyModel, Guid>
    {
    }
}

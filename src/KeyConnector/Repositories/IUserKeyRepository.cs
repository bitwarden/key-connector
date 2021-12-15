using System;
using Bit.KeyConnector.Models;

namespace Bit.KeyConnector.Repositories
{
    public interface IUserKeyRepository : IRepository<UserKeyModel, Guid>
    {
    }
}

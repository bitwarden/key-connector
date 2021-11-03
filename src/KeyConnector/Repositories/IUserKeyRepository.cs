using Bit.KeyConnector.Models;
using System;

namespace Bit.KeyConnector.Repositories
{
    public interface IUserKeyRepository : IRepository<UserKeyModel, Guid>
    {
    }
}

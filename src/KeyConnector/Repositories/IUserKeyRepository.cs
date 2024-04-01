using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.KeyConnector.Models;

namespace Bit.KeyConnector.Repositories
{
    public interface IUserKeyRepository : IRepository<UserKeyModel, Guid>
    {
        Task<List<UserKeyModel>> ReadAllAsync();
    }
}

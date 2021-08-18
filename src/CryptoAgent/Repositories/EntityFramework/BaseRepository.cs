using System;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.CryptoAgent.Repositories.EntityFramework
{
    public abstract class BaseRepository
    {
        public BaseRepository(IServiceScopeFactory serviceScopeFactory)
        {
            ServiceScopeFactory = serviceScopeFactory;
        }

        protected IServiceScopeFactory ServiceScopeFactory { get; private set; }

        protected DatabaseContext GetDatabaseContext(IServiceScope serviceScope)
        {
            return serviceScope.ServiceProvider.GetRequiredService<DatabaseContext>();
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.CryptoAgent.Repositories.EntityFramework
{
    public class ApplicationDataRepository : BaseRepository, IApplicationDataRepository
    {
        public ApplicationDataRepository(IServiceScopeFactory serviceScopeFactory)
            : base(serviceScopeFactory)
        { }

        public Task<string> ReadSymmetricKeyAsync()
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            return Task.FromResult(dbContext.ApplicationDatas.FirstOrDefault().SymmetricKey);
        }

        public async Task UpdateSymmetricKeyAsync(string key)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            if (dbContext.ApplicationDatas.FirstOrDefault() == null)
            {
                await dbContext.AddAsync(new ApplicationData
                {
                    SymmetricKey = key
                });
            }
            else
            {
                dbContext.ApplicationDatas.FirstOrDefault().SymmetricKey = key;
            }
            await dbContext.SaveChangesAsync();
        }
    }
}

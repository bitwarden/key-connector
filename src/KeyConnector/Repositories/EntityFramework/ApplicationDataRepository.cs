using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.KeyConnector.Repositories.EntityFramework
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
            return Task.FromResult(dbContext.ApplicationDatas.FirstOrDefault()?.SymmetricKey);
        }

        public async Task UpdateSymmetricKeyAsync(string key)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            var data = dbContext.ApplicationDatas.FirstOrDefault();
            if (data == null)
            {
                await dbContext.AddAsync(new ApplicationData
                {
                    SymmetricKey = key
                });
            }
            else
            {
                data.SymmetricKey = key;
            }
            await dbContext.SaveChangesAsync();
        }
    }
}

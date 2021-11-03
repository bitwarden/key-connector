using Bit.KeyConnector.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Repositories.EntityFramework
{
    public class UserKeyRepository : BaseRepository, IUserKeyRepository
    {
        public UserKeyRepository(IServiceScopeFactory serviceScopeFactory)
            : base(serviceScopeFactory)
        { }

        public virtual async Task CreateAsync(UserKeyModel item)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            var entity = new UserKey(item);
            await dbContext.AddAsync(entity);
            await dbContext.SaveChangesAsync();
        }

        public virtual async Task<UserKeyModel> ReadAsync(Guid id)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            var entity = await dbContext.UserKeys.FindAsync(id);
            return entity?.ToUserKeyModel();
        }

        public virtual async Task UpdateAsync(UserKeyModel item)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            var entity = await dbContext.UserKeys.FindAsync(item.Id);
            if (entity != null)
            {
                entity.Load(item);
                await dbContext.SaveChangesAsync();
            }
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            using var scope = ServiceScopeFactory.CreateScope();
            var dbContext = GetDatabaseContext(scope);
            var entity = await dbContext.UserKeys.FindAsync(id);
            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync();
        }
    }
}

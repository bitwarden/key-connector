using System;

namespace Bit.CryptoAgent.Models
{
    public class UserKeyModel : BaseUserKeyModel, IStoredItem<Guid>
    {
        public Guid Id { get; set; }
    }

    public abstract class BaseUserKeyModel
    {
        public string Key { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public DateTime? RevisionDate { get; set; }
        public DateTime? LastAccessDate { get; set; }
    }
}

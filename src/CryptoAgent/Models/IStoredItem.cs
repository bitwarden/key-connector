using System;

namespace Bit.CryptoAgent.Models
{
    public interface IStoredItem<TId> where TId : IEquatable<TId>
    {
        public TId Id { get; set; }
    }
}

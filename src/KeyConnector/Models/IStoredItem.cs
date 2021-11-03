using System;

namespace Bit.KeyConnector.Models
{
    public interface IStoredItem<TId> where TId : IEquatable<TId>
    {
        public TId Id { get; set; }
    }
}

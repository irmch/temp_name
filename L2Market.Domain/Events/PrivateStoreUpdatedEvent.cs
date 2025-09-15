using System.Collections.Generic;
using L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;

namespace L2Market.Domain.Events
{
    public class PrivateStoreUpdatedEvent
    {
        public List<PrivateStoreItem> Items { get; }
        public uint? ProcessId { get; }

        public PrivateStoreUpdatedEvent(List<PrivateStoreItem> items, uint? processId = null)
        {
            Items = items;
            ProcessId = processId;
        }
    }
}

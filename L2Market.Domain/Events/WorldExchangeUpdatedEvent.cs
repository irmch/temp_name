using System.Collections.Generic;
using L2Market.Domain.Entities.WorldExchangeItemListPacket;

namespace L2Market.Domain.Events
{
    public class WorldExchangeUpdatedEvent
    {
        public List<WorldExchangeItemInfo> Items { get; }
        public uint? ProcessId { get; }
        public int? Category { get; }

        public WorldExchangeUpdatedEvent(List<WorldExchangeItemInfo> items, uint? processId = null, int? category = null)
        {
            Items = items;
            ProcessId = processId;
            Category = category;
        }
    }
}

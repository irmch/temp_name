using System.Collections.Generic;
using L2Market.Domain.Entities.ExResponseCommissionListPacket;

namespace L2Market.Domain.Events
{
    public class CommissionUpdatedEvent
    {
        public List<CommissionItem> Items { get; }
        public uint? ProcessId { get; }

        public CommissionUpdatedEvent(List<CommissionItem> items, uint? processId = null)
        {
            Items = items;
            ProcessId = processId;
        }
    }
}

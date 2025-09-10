namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Предмет в комиссии
    /// </summary>
    public class CommissionItem
    {
        private readonly long _commissionId;
        private readonly long _pricePerUnit;
        private readonly int _commissionItemType;
        private readonly int _durationType;
        private readonly int _endTime;
        private readonly string? _sellerName;
        private readonly ItemInfo _itemInfo;

        public long CommissionId => _commissionId;
        public long PricePerUnit => _pricePerUnit;
        public int CommissionItemType => _commissionItemType;
        public int DurationType => _durationType;
        public int EndTime => _endTime;
        public string? SellerName => _sellerName;
        public ItemInfo ItemInfo => _itemInfo;

        public CommissionItem(
            long commissionId,
            long pricePerUnit,
            int commissionItemType,
            int durationType,
            int endTime,
            string? sellerName,
            ItemInfo itemInfo)
        {
            _commissionId = commissionId;
            _pricePerUnit = pricePerUnit;
            _commissionItemType = commissionItemType;
            _durationType = durationType;
            _endTime = endTime;
            _sellerName = sellerName;
            _itemInfo = itemInfo;
        }

        public override string ToString()
        {
            return $"CommissionItem(id={_commissionId}, price={_pricePerUnit}, item={_itemInfo})";
        }
    }
}

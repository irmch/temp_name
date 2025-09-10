using System.Collections.Generic;
using System;

namespace L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;

public class PrivateStoreItem
{
    private readonly string _vendorName;
    private readonly int _vendorObjectId;
    private readonly int _storeType;
    private readonly long _price;
    private readonly int _vendorX;
    private readonly int _vendorY;
    private readonly int _vendorZ;
    private readonly ItemInfo _itemInfo;

    public string VendorName => _vendorName;
    public int VendorObjectId => _vendorObjectId;
    public int StoreType => _storeType;
    public long Price => _price;
    public int VendorX => _vendorX;
    public int VendorY => _vendorY;
    public int VendorZ => _vendorZ;
    public ItemInfo ItemInfo => _itemInfo;

    public PrivateStoreItem(
        string vendorName,
        int vendorObjectId,
        int storeType,
        long price,
        int vendorX,
        int vendorY,
        int vendorZ,
        ItemInfo itemInfo)
    {
        _vendorName = vendorName;
        _vendorObjectId = vendorObjectId;
        _storeType = storeType;
        _price = price;
        _vendorX = vendorX;
        _vendorY = vendorY;
        _vendorZ = vendorZ;
        _itemInfo = itemInfo;
    }

    public override string ToString()
    {
        return $"PrivateStoreItem(vendor={_vendorName}, price={_price:N0}, item={_itemInfo})";
    }
}

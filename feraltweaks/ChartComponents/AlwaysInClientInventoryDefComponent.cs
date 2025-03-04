using System;
using FeralTweaks.Mods.Charts;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using LitJson;

public class AlwaysInClientInventoryDefComponent : FeralTweaksChartDefComponent
{
    public AlwaysInClientInventoryDefComponent()
    {
    }

    public AlwaysInClientInventoryDefComponent(IntPtr pointer) : base(pointer)
    {
    }

    [HideFromIl2Cpp]
    public override void Deserialize(Dictionary<string, object> json)
    {
        JsonConvert.PopulateObject(JsonConvert.SerializeObject(json), this);
    }

    public bool requireOwnedItems;
    public string[] requiredOwnedItemDefIDs;

    public int itemType;
    public Dictionary<string, Dictionary<string, object>> components = new Dictionary<string, Dictionary<string, object>>();

    public class ItemDataDummy
    {
        public int type;
        public string id;
        public string defId;
        public Dictionary<string, Dictionary<string, object>> components = new Dictionary<string, Dictionary<string, object>>();
    }

    /// <summary>
    /// Adds the current item to the given target inventory
    /// </summary>
    /// <param name="inventory">Inventory to add the item to</param>
    public void AddToInventory(Inventory inventory)
    {
        // Create ID
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("localitems:" + def.defID));
        hash[6] &= 0x0f;
        hash[6] |= 0x30;
        hash[8] &= 0x3f;
        hash[8] |= 0x80;
        byte temp = hash[6];
        hash[6] = hash[7];
        hash[7] = temp;
        temp = hash[4];
        hash[4] = hash[5];
        hash[5] = temp;
        temp = hash[0];
        hash[0] = hash[3];
        hash[3] = temp;
        temp = hash[1];
        hash[1] = hash[2];
        hash[2] = temp;
        string itemID = new Guid(hash).ToString().ToLower();

        // Find
        if (inventory.GetById(itemID) != null)
            return;

        // Create object
        ItemDataDummy dum = new ItemDataDummy();
        dum.type = itemType;
        dum.id = itemID;
        dum.defId = def.defID;
        dum.components = components;

        // Serialize
        JsonData jsonData = JsonMapper.ToObject<JsonData>(JsonConvert.SerializeObject(dum));
        Item itm = new Item(jsonData);

        // Add
        inventory.AddFromServer(itm);
        CoreMessageManager.SendMessage<InventoryItemAddedEvent>(InventoryItemAddedEvent.Create(itm, 1));
    }
}
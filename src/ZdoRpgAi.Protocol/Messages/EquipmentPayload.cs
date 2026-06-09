using System.Collections.Generic;

namespace ZdoRpgAi.Protocol.Messages;

public class EquippedItem
{
    public string Slot { get; set; } = "";
    public string ItemId { get; set; } = "";
}

public class EquipmentMessage
{
    public string Id { get; set; } = "";
    public List<EquippedItem> Items { get; set; } = new();
}

public class EquipItemPayload
{
    public string CharacterId { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string Slot { get; set; } = "";

    public EquipItemPayload(string characterId, string itemId, string slot)
    {
        CharacterId = characterId;
        ItemId = itemId;
        Slot = slot;
    }
}

using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client collects kinah or item attachment from a mail. Opcode 0x12A.</summary>
public sealed class CM_GET_MAIL_ATTACHMENT : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly IMailDao           _mailDao;
    private readonly IItemDao           _itemDao;

    private int  _mailId;
    private byte _attachmentType; // 0=item, 1=kinah

    public CM_GET_MAIL_ATTACHMENT(GsClientConnection conn, IMailDao mailDao, IItemDao itemDao)
    {
        _conn    = conn;
        _mailDao = mailDao;
        _itemDao = itemDao;
    }

    public override void Read(ref PacketReader r)
    {
        _mailId          = r.ReadD();
        _attachmentType  = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var mail = await _mailDao.GetByIdAsync(_mailId, ct);
        if (mail is null || mail.ReceiverId != player.ObjectId || mail.AttachmentTaken) return;

        const int KinahId = 182400001;

        if (_attachmentType == 1 && mail.AttachedKinah > 0)
        {
            // Transfer kinah
            var kinah = player.Inventory.FindByItemId(KinahId);
            if (kinah is not null)
            {
                kinah.Count += mail.AttachedKinah;
            }
            else
            {
                var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
                kinah = new Item { UniqueId = uniqueId, ItemId = KinahId, Count = mail.AttachedKinah, Slot = -1 };
                player.Inventory.Add(kinah);
            }
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        }
        else if (_attachmentType == 0 && mail.AttachedItemId != 0)
        {
            // Transfer item (recreate from templateId + count)
            var uniqueId = await _itemDao.NextUniqueIdAsync(ct);
            var item = new Item { UniqueId = uniqueId, ItemId = mail.AttachedItemId, Count = mail.AttachedCount, Slot = -1 };
            player.Inventory.Add(item);
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }

        await _mailDao.MarkAttachmentTakenAsync(_mailId, ct);
        await _conn.SendAsync(new SM_MAIL_SERVICE(_mailId, attachmentTaken: true), ct);
    }
}

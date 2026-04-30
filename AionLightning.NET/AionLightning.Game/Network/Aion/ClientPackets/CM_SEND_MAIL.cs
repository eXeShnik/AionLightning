using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.Model.Mail;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client sends in-game mail. Opcode 0x126.</summary>
public sealed class CM_SEND_MAIL : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly IPlayerDao               _playerDao;
    private readonly IMailDao                 _mailDao;
    private readonly IItemDao                 _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _recipientName = string.Empty;
    private string _title         = string.Empty;
    private string _message       = string.Empty;
    private int    _itemObjId;
    private int    _itemCount;
    private int    _kinahCount;
    private byte   _letterType;

    public CM_SEND_MAIL(GsClientConnection conn, IPlayerDao playerDao,
        IMailDao mailDao, IItemDao itemDao, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _playerDao    = playerDao;
        _mailDao      = mailDao;
        _itemDao      = itemDao;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r)
    {
        _recipientName = r.ReadS();
        _title         = r.ReadS();
        _message       = r.ReadS();
        _itemObjId     = r.ReadD();
        _itemCount     = r.ReadD();
        r.ReadD();          // unk
        _kinahCount    = r.ReadD();
        r.ReadD();          // unk
        _letterType    = r.ReadC();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var sender = _conn.ActivePlayer;
        if (sender is null) return;

        var recipient = await _playerDao.FindByNameAsync(_recipientName, ct);
        if (recipient is null)
        {
            await _conn.SendAsync(new SM_MAIL_SERVICE(sendResult: 3), ct); // not found
            return;
        }

        // Deduct attached kinah
        int kinahToSend = Math.Max(0, _kinahCount);
        if (kinahToSend > 0)
        {
            const int KinahId = 182400001;
            var kinah = sender.Inventory.FindByItemId(KinahId);
            if (kinah is null || kinah.Count < kinahToSend)
            {
                await _conn.SendAsync(new SM_MAIL_SERVICE(sendResult: 2), ct); // not enough kinah
                return;
            }
            kinah.Count -= kinahToSend;
            await _itemDao.SaveAllAsync(sender.ObjectId, sender.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([kinah]), ct);
        }

        // Detach item from sender inventory if attaching one
        int attachedItemId = 0;
        long attachedCount = 0;
        if (_itemObjId != 0)
        {
            var item = sender.Inventory.Get(_itemObjId);
            if (item is not null && !item.IsEquipped)
            {
                int count = Math.Min(_itemCount > 0 ? _itemCount : (int)item.Count, (int)item.Count);
                attachedItemId = item.ItemId;
                attachedCount  = count;
                if (item.Count <= count)
                {
                    sender.Inventory.Remove(item.UniqueId);
                    await _itemDao.DeleteAsync(item.UniqueId, ct);
                    await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
                }
                else
                {
                    item.Count -= count;
                    await _itemDao.SaveAllAsync(sender.ObjectId, sender.Inventory.All, ct);
                    await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
                }
            }
        }

        var mail = new MailEntry
        {
            SenderId       = sender.ObjectId,
            SenderName     = sender.Name,
            ReceiverId     = recipient.ObjectId,
            ReceiverName   = recipient.Name,
            Title          = _title,
            Message        = _message,
            AttachedItemId = attachedItemId,
            AttachedCount  = attachedCount,
            AttachedKinah  = kinahToSend,
            LetterType     = _letterType,
            SendDate       = DateTime.UtcNow,
        };
        await _mailDao.InsertAsync(mail, ct);

        // Notify recipient if online
        var recipientConn = _connRegistry.GetByName(recipient.Name);
        if (recipientConn?.ActivePlayer is not null)
        {
            var mails = await _mailDao.GetReceivedMailsAsync(recipient.ObjectId, ct);
            int unread = mails.Count(m => !m.IsRead);
            await recipientConn.SendAsync(new SM_MAIL_SERVICE(mails.Count, unread), ct);
        }

        await _conn.SendAsync(new SM_MAIL_SERVICE(sendResult: 0), ct); // OK
    }
}

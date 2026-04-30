using AionLightning.Commons.Network;
using AionLightning.Game.Model.Mail;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Handles all mail operations (serviceId sub-type switch). Opcode 0xA1.
/// Java MailServicePacket pattern: one packet class with multiple serviceId modes.
/// </summary>
public sealed class SM_MAIL_SERVICE : AionServerPacket
{
    public enum ServiceId : byte
    {
        MailboxState   = 0,
        SendResult     = 1,
        LetterList     = 2,
        LetterContent  = 3,
        AttachmentTaken = 5,
        LetterDeleted  = 6,
    }

    private readonly ServiceId              _serviceId;
    private readonly IReadOnlyList<MailEntry>? _letters;
    private readonly MailEntry?             _letter;
    private readonly int                    _totalCount;
    private readonly int                    _unreadCount;
    private readonly byte                   _sendResult;
    private readonly int[]?                 _deletedIds;
    private readonly int                    _attachmentMailId;

    // serviceId=0: mailbox state on login
    public SM_MAIL_SERVICE(int totalCount, int unreadCount) : base(0xA1)
    {
        _serviceId   = ServiceId.MailboxState;
        _totalCount  = totalCount;
        _unreadCount = unreadCount;
    }

    // serviceId=1: send result (0=OK)
    public SM_MAIL_SERVICE(byte sendResult) : base(0xA1)
    {
        _serviceId  = ServiceId.SendResult;
        _sendResult = sendResult;
    }

    // serviceId=2: letter list
    public SM_MAIL_SERVICE(int receiverObjectId, IReadOnlyList<MailEntry> letters) : base(0xA1)
    {
        _serviceId   = ServiceId.LetterList;
        _letter      = new MailEntry { Id = receiverObjectId }; // abuse field for objectId
        _letters     = letters;
        _totalCount  = letters.Count;
        _unreadCount = letters.Count(l => !l.IsRead);
    }

    // serviceId=3: letter content (read)
    public SM_MAIL_SERVICE(MailEntry letter, int totalCount, int unreadCount) : base(0xA1)
    {
        _serviceId   = ServiceId.LetterContent;
        _letter      = letter;
        _totalCount  = totalCount;
        _unreadCount = unreadCount;
    }

    // serviceId=5: attachment taken
    public SM_MAIL_SERVICE(int mailId, bool attachmentTaken) : base(0xA1)
    {
        _serviceId        = ServiceId.AttachmentTaken;
        _attachmentMailId = mailId;
    }

    // serviceId=6: letter deleted
    public SM_MAIL_SERVICE(int[] deletedIds, int totalCount, int unreadCount) : base(0xA1)
    {
        _serviceId   = ServiceId.LetterDeleted;
        _deletedIds  = deletedIds;
        _totalCount  = totalCount;
        _unreadCount = unreadCount;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteC((byte)_serviceId);
        switch (_serviceId)
        {
            case ServiceId.MailboxState:
                w.WriteH((short)_totalCount);
                w.WriteH((short)_unreadCount);
                w.WriteH(0); // express unread
                w.WriteH(0); // black cloud unread
                break;

            case ServiceId.SendResult:
                w.WriteC(_sendResult);
                break;

            case ServiceId.LetterList:
                WriteLetterList(ref w);
                break;

            case ServiceId.LetterContent:
                WriteLetterContent(ref w);
                break;

            case ServiceId.AttachmentTaken:
                w.WriteD(_attachmentMailId);
                w.WriteC(1); // attachment type claimed
                w.WriteC(1); // unk
                break;

            case ServiceId.LetterDeleted:
                // total + unread packed into one D as per Java: total + unread*0x10000
                w.WriteD(_totalCount + _unreadCount * 0x10000);
                w.WriteD(0); // express + BC unread
                w.WriteH((short)(_deletedIds?.Length ?? 0));
                if (_deletedIds is not null)
                    foreach (var id in _deletedIds)
                        w.WriteD(id);
                break;
        }
    }

    private void WriteLetterList(ref PacketWriter w)
    {
        int receiverObjId = _letter?.Id ?? 0;
        var letters = _letters ?? Array.Empty<MailEntry>();
        w.WriteD(receiverObjId);
        w.WriteC(0);
        w.WriteH((short)(-letters.Count)); // negated count (Aion quirk)
        foreach (var l in letters)
        {
            w.WriteD(l.Id);
            w.WriteS(l.SenderName);
            w.WriteS(l.Title);
            w.WriteC(l.IsRead ? (byte)1 : (byte)0);
            // attached item summary (templateId + objectId placeholder)
            w.WriteD(l.AttachedItemId);
            w.WriteD(0); // attached item objectId (not tracked per-row)
            w.WriteQ(l.AttachedKinah);
            w.WriteC(l.LetterType);
        }
    }

    private void WriteLetterContent(ref PacketWriter w)
    {
        var l = _letter!;
        w.WriteD(l.ReceiverId);
        w.WriteD(_totalCount + _unreadCount * 0x10000);
        w.WriteD(0); // express+BC unread
        w.WriteD(l.Id);
        w.WriteD(l.ReceiverId);
        w.WriteS(l.SenderName);
        w.WriteS(l.Title);
        w.WriteS(l.Message);

        if (l.AttachedItemId != 0 && !l.AttachmentTaken)
        {
            // Simplified item blob: templateId as objectId placeholder + minimal general blob
            w.WriteD(l.AttachedItemId); // item objectId placeholder
            w.WriteD(l.AttachedItemId); // templateId
            w.WriteD(1);                // unk
            w.WriteD(0);                // unk
            w.WriteD(0);                // nameId
            // GeneralInfoBlob
            w.WriteC(0x00);             // blob type
            w.WriteH(0);                // itemMask
            w.WriteQ(l.AttachedCount);  // count
            w.WriteC(0); w.WriteC(0);   // creator name empty
            w.WriteC(0);
            w.WriteD(0); w.WriteD(0); w.WriteD(0);
            w.WriteH(0); w.WriteD(0);
            w.WriteH(-1);               // slot = bag
            w.WriteC(0);
        }
        else
        {
            w.WriteQ(0); w.WriteQ(0); w.WriteD(0); // no item
        }

        w.WriteD((int)l.AttachedKinah);
        w.WriteD(0); // AP reward
        w.WriteC(0); // unk
        w.WriteD((int)new DateTimeOffset(l.SendDate).ToUnixTimeSeconds());
        w.WriteC(l.LetterType);
    }
}

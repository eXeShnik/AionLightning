namespace AionLightning.Game.Model.Mail;

public sealed class MailEntry
{
    public int      Id             { get; init; }
    public int      SenderId       { get; init; }
    public string   SenderName     { get; init; } = string.Empty;
    public int      ReceiverId     { get; init; }
    public string   ReceiverName   { get; init; } = string.Empty;
    public string   Title          { get; init; } = string.Empty;
    public string   Message        { get; init; } = string.Empty;
    public int      AttachedItemId { get; init; }   // template id; 0 = none
    public long     AttachedCount  { get; init; }
    public long     AttachedKinah  { get; init; }
    public byte     LetterType     { get; init; }   // 0 = NORMAL, 1 = EXPRESS
    public DateTime SendDate       { get; init; }
    public bool     IsRead         { get; set; }
    public bool     AttachmentTaken { get; set; }
}

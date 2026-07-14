namespace AionLightning.Game.Model.AutoGroup;

/// <summary>
/// Static queue definition loaded from <c>auto_group.xml</c> (Java <c>dataholders.AutoGroupData</c> /
/// <c>model.autogroup.AutoGroup</c>). <see cref="MaskId"/> is the id the client sends in CM_AUTO_GROUP
/// and the key used to look up per-queue capacity/category metadata in <see cref="AutoGroupMetadata"/>.
/// </summary>
public sealed record AutoGroupTemplate(
    int MaskId,
    int InstanceMapId,
    int NameId,
    int TitleId,
    int MinLevel,
    int MaxLevel,
    bool RegisterNew,
    bool RegisterQuick,
    bool RegisterGroup,
    IReadOnlyList<int> NpcIds);

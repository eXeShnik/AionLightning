namespace AionLightning.Game.Ai;

/// <summary>
/// Binds an <see cref="NpcAi2"/> subclass to an npc_templates.xml "ai" name (Java
/// <c>@AIName("name")</c>). The AI engine reflects over compiled <c>Scripts/ai/**</c> scripts and
/// registers each annotated class as the AI factory for its name — the exact analog of
/// <see cref="AionLightning.Game.Instance.InstanceIdAttribute"/> for instance handlers.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AiNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

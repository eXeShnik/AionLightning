using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Challenge;

/// <summary>Port of Java model.templates.challenge.ChallengeQuestTemplate — one &lt;quest&gt; entry of a
/// challenge task, declaring how many times it must be completed and how much score it's worth.</summary>
public sealed class ChallengeQuestTemplate
{
    [XmlAttribute("id")]           public int Id          { get; set; }
    [XmlAttribute("score")]        public int Score        { get; set; }
    [XmlAttribute("repeat_count")] public int RepeatCount   { get; set; }
}

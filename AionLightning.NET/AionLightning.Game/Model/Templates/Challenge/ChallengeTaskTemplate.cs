using System.Xml.Serialization;

namespace AionLightning.Game.Model.Templates.Challenge;

/// <summary>
/// Port of Java model.templates.challenge.ChallengeTaskTemplate — one &lt;task&gt; element of
/// data/static_data/quest_data/challenge_tasks.xml: a legion or town objective built from one or more
/// challenge quests, optionally chained after a prerequisite task (<see cref="PrevTask"/>) and optionally
/// repeatable once completed.
/// </summary>
[XmlRoot("task")]
public sealed class ChallengeTaskTemplate
{
    [XmlAttribute("id")]             public int  Id            { get; set; }
    [XmlAttribute("type")]           public ChallengeType Type { get; set; }
    [XmlAttribute("race")]           public Race Race           { get; set; } = Race.PC_ALL;
    [XmlAttribute("min_level")]      public int  MinLevel       { get; set; }
    [XmlAttribute("max_level")]      public int  MaxLevel       { get; set; }
    [XmlAttribute("prev_task")]      public int  PrevTask       { get; set; }
    [XmlAttribute("name_id")]        public int  NameId         { get; set; }
    [XmlAttribute("repeat")]         public bool Repeat         { get; set; }
    [XmlAttribute("town_residence")] public bool TownResidence  { get; set; }

    [XmlElement("quest")]   public List<ChallengeQuestTemplate> Quests { get; set; } = new();
    [XmlElement("contrib")] public List<ContributionReward>     Contrib { get; set; } = new();
    [XmlElement("reward")]  public ChallengeReward               Reward { get; set; } = new();

    public bool HasPrevTask => PrevTask != 0;
}

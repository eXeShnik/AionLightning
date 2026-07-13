namespace AionLightning.Game.Model.Templates.Housing;

/// <summary>&lt;sale&gt; child element of a &lt;land&gt; in houses.xml (Java model.templates.housing.Sale) —
/// the minimum character level and default auction/point prices for houses on this land.</summary>
public sealed record Sale(int MinLevel, long GoldPrice, int PointPrice);

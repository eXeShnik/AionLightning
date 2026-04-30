using AionLightning.Commons.Network;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;
using AionLightning.Game.Model.Skill;
using AionLightning.Game.Model.Templates.Item;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client uses a consumable item. Opcode 0xC7.</summary>
public sealed class CM_USE_ITEM : AionClientPacket
{
    // Maps skilluse skill IDs from item_templates.xml to (hpRestorePercent, mpRestorePercent).
    // Values represent percentage of max HP/MP restored instantly.
    private static readonly Dictionary<int, (int Hp, int Mp)> SkillEffects = new()
    {
        { 10202, (15,  0) }, // Minor Life Elixir   lv10
        { 10203, (20,  0) }, // Lesser Life Elixir  lv20
        { 10204, (25,  0) }, // Regular Life Elixir lv30
        { 10205, (30,  0) }, // Greater Life Elixir lv40
        { 10206, (35,  0) }, // Pure Life Elixir    lv50
        { 10207, (40,  0) }, // Odium Life Elixir   lv55
        { 10208, (45,  0) }, // Unique Life Elixir  lv60
        { 10262, ( 0, 15) }, // Minor Mana Elixir   lv10
        { 10263, ( 0, 20) }, // Lesser Mana Elixir  lv20
        { 10264, ( 0, 25) }, // Regular Mana Elixir lv30
        { 10265, ( 0, 30) }, // Greater Mana Elixir lv40
        { 10266, ( 0, 35) }, // Pure Mana Elixir    lv50
        { 10267, ( 0, 40) }, // Odium Mana Elixir   lv55
        { 10268, ( 0, 45) }, // Unique Mana Elixir  lv60
    };

    private readonly GsClientConnection _conn;
    private readonly IItemDao           _itemDao;
    private readonly IDataManager       _dataManager;
    private readonly IRecipeDao         _recipeDao;

    private int _uniqueItemId;
    private int _type;
    private int _targetItemId;

    public CM_USE_ITEM(GsClientConnection conn, IItemDao itemDao, IDataManager dataManager, IRecipeDao recipeDao)
    {
        _conn        = conn;
        _itemDao     = itemDao;
        _dataManager = dataManager;
        _recipeDao   = recipeDao;
    }

    public override void Read(ref PacketReader r)
    {
        _uniqueItemId = r.ReadD();
        _type         = r.ReadC();
        if (_type == 2) _targetItemId = r.ReadD();
        if (_type == 6) _targetItemId = r.ReadD();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        var item = player.Inventory.Get(_uniqueItemId);
        if (item is null) return;

        var template = _dataManager.Items.GetTemplate(item.ItemId);
        if (template is null) return;

        // Skill book — teaches a skill permanently
        if (template.SkillLearnId is int learnSkillId)
        {
            await HandleSkillBookAsync(player, item, template, learnSkillId, ct);
            return;
        }

        // Recipe book — teaches a crafting recipe permanently
        if (template.CraftLearnRecipeId is int recipeId)
        {
            await HandleRecipeBookAsync(player, item, recipeId, ct);
            return;
        }

        if (template.UseSkillId is not int skillId) return;
        if (!SkillEffects.TryGetValue(skillId, out var effect)) return;

        // Apply HP restore
        if (effect.Hp > 0 && player.MaxHp > 0)
        {
            int restore = Math.Max(1, player.MaxHp * effect.Hp / 100);
            player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + restore);
            await _conn.SendAsync(
                new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, skillId, restore, SM_ATTACK_STATUS.LogId.Heal), ct);
        }

        // Apply MP restore
        if (effect.Mp > 0 && player.MaxMp > 0)
        {
            int restore = Math.Max(1, player.MaxMp * effect.Mp / 100);
            player.CurrentMp = Math.Min(player.MaxMp, player.CurrentMp + restore);
            await _conn.SendAsync(
                new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, skillId, restore, SM_ATTACK_STATUS.LogId.MpHeal), ct);
        }

        var statTpl = _dataManager.PlayerStats.GetTemplate(player.PlayerClass, player.Level);
        await _conn.SendAsync(new SM_STATS_INFO(player, statTpl, _dataManager.ExpTable), ct);

        // Consume one charge
        item.Count--;
        if (item.Count <= 0)
        {
            player.Inventory.Remove(item.UniqueId);
            await _itemDao.DeleteAsync(item.UniqueId, ct);
            await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
        }
        else
        {
            await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
            await _conn.SendAsync(new SM_INVENTORY_ADD_ITEM([item]), ct);
        }
    }

    private async ValueTask HandleSkillBookAsync(Player player, Item item, ItemTemplate template,
        int skillId, CancellationToken ct)
    {
        var learnAction = template.Actions!.SkillLearn!;

        // Class restriction: "ALL" or empty means anyone can use it
        if (!string.IsNullOrEmpty(learnAction.ClassRestriction)
            && learnAction.ClassRestriction != "ALL"
            && !player.PlayerClass.ToString().Equals(learnAction.ClassRestriction, StringComparison.OrdinalIgnoreCase))
            return;

        // Level restriction
        if (player.Level < learnAction.RequiredLevel) return;

        // Already known
        if (player.Skills.IsPresent(skillId)) return;

        int skillLevel = _dataManager.SkillTree.GetMaxSkillLevel(skillId, player.PlayerClass, player.Race, player.Level);
        player.Skills.AddSkill(skillId, skillLevel);

        string skillName = _dataManager.SkillTree.GetSkillName(skillId) ?? template.Name;
        await _conn.SendAsync(new SM_SKILL_LIST(
            [new PlayerSkillEntry(skillId, skillLevel)],
            isNew: true, msgId: 1300050, skillName: skillName, skillLevel: skillLevel), ct);

        // Skill books are non-stackable — always delete on use
        player.Inventory.Remove(item.UniqueId);
        await _itemDao.DeleteAsync(item.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
    }

    private async ValueTask HandleRecipeBookAsync(Player player, Item item, int recipeId, CancellationToken ct)
    {
        // Already known
        if (player.KnownRecipes.Contains(recipeId)) return;

        // Must exist in data
        if (_dataManager.Recipes.GetTemplate(recipeId) is null) return;

        player.KnownRecipes.Add(recipeId);
        await _recipeDao.AddRecipeAsync(player.ObjectId, recipeId, ct);
        await _conn.SendAsync(new SM_RECIPE_LIST(player.KnownRecipes), ct);

        // Recipe books are non-stackable — always delete on use
        player.Inventory.Remove(item.UniqueId);
        await _itemDao.DeleteAsync(item.UniqueId, ct);
        await _conn.SendAsync(new SM_DELETE_ITEM(item.UniqueId), ct);
    }
}

using AionLightning.Game.DataHolders;
using AionLightning.Game.Model.Pet;
using AionLightning.Game.Model.Templates.Pet;

namespace AionLightning.Game.Services;

/// <summary>
/// Feed-minigame point math (Java <c>services/toypet/PetFeedCalculator</c>). Builds its per-item-level,
/// per-full-count point tables from the loaded pet_feed.xml flavours the first time it's used
/// (<see cref="EnsureInitialized"/>) — Java built the equivalent tables in a static initializer that
/// read the already-loaded <c>DataManager.PET_FEED_DATA</c>; the lazy warm-up here achieves the same
/// thing without a static-init-order dependency on the DI-constructed <see cref="IDataManager"/>.
/// </summary>
public static class PetFeedCalculator
{
    private const int ItemMaxLevel = 60;

    private static short[] _fullCounts = [];
    private static byte[] _itemLevels = [];
    private static int[][] _pointValues = [];
    private static bool _initialized;
    private static readonly object InitLock = new();

    public static void EnsureInitialized(PetFeedData feedData)
    {
        if (_initialized) return;
        lock (InitLock)
        {
            if (_initialized) return;
            Build(feedData);
            _initialized = true;
        }
    }

    private static void Build(PetFeedData feedData)
    {
        var counts = new SortedSet<short>();
        foreach (var flavour in feedData.Flavours)
            if (flavour.FullCount > 0)
                counts.Add((short)(flavour.FullCount & 0xFFFF));
        _fullCounts = counts.ToArray();

        _itemLevels = new byte[ItemMaxLevel / 5];
        _itemLevels[0] = 5;
        for (int j = 1; j < _itemLevels.Length; j++)
            _itemLevels[j] = (byte)(_itemLevels[j - 1] + 5);

        _pointValues = new int[_itemLevels.Length][];
        for (int i = 0; i < _pointValues.Length; i++)
            _pointValues[i] = new int[_fullCounts.Length];

        foreach (byte levelByte in _itemLevels)
        {
            short level = (short)(levelByte & 0xFF);
            if (level < 10) continue;

            int countIndex = 0;
            foreach (short countValue in _fullCounts)
            {
                int finalLevel = level;
                if (finalLevel % 5 == 0) finalLevel--;
                int pointLevel = _itemLevels[finalLevel / 5];
                int feedPoints = Math.Max(0, pointLevel - 5) / 5 * 8;
                _pointValues[finalLevel / 5][countIndex++] = GetPoints(feedPoints, countValue);
            }
        }
    }

    private static int GetPoints(int feedPoints, int maxFeedCount)
    {
        int points = 0, state = 0, consumed = 0;
        while (consumed < maxFeedCount)
        {
            bool needSwitch = (state == 0 && consumed > maxFeedCount * 0.5f)
                || (state == 1 && consumed > maxFeedCount * 0.8f)
                || (state == 2 && consumed > maxFeedCount * 1.05f);
            int oldPoints = points;

            points += feedPoints;
            if (needSwitch)
            {
                state++;
                if ((state == 1 && consumed <= 0.487f * maxFeedCount) || (state == 2 && consumed <= 0.78f * maxFeedCount))
                {
                    state--;
                    points = oldPoints;
                }
            }
            consumed++;
        }
        return points;
    }

    public static void UpdatePetFeedProgress(PetFeedProgress progress, int itemLevel, int maxFeedCount)
    {
        var currHungryLevel = progress.HungryLevel;
        if (progress.IsLovedFeeded)
        {
            if (progress.LovedFoodRemaining == 0) return;
            progress.HungryLevel = PetHungryLevel.Full;
            progress.IncrementCount(true);
            return;
        }

        int oldPoints = progress.TotalPoints;
        bool needSwitch = (currHungryLevel == PetHungryLevel.Hungry && progress.RegularCount > maxFeedCount * 0.5f)
            || (currHungryLevel == PetHungryLevel.Content && progress.RegularCount > maxFeedCount * 0.8f)
            || (currHungryLevel == PetHungryLevel.SemiFull && progress.RegularCount > maxFeedCount * 1.05f);

        if (!needSwitch)
        {
            int finalLevel = itemLevel;
            if (finalLevel % 5 == 0) finalLevel--;
            byte pointLevel = _itemLevels[finalLevel / 5];
            byte pointsEarned = (byte)(Math.Max(0, pointLevel - 5) / 5 * 8);
            progress.TotalPoints += pointsEarned;
        }

        if (needSwitch)
        {
            var nextLevel = progress.HungryLevel.GetNextValue();
            if ((nextLevel == PetHungryLevel.Content && progress.RegularCount <= 0.487f * maxFeedCount)
                || (nextLevel == PetHungryLevel.SemiFull && progress.RegularCount <= 0.78f * maxFeedCount))
            {
                progress.TotalPoints = oldPoints;
            }
            else
            {
                progress.HungryLevel = nextLevel;
            }
        }

        progress.IncrementCount(false);
    }

    public static PetFeedResult? GetReward(int fullCount, PetRewards rewardGroup, PetFeedProgress progress, int playerLevel, ItemData itemData)
    {
        if (progress.HungryLevel != PetHungryLevel.Full || rewardGroup.Results.Count == 0) return null;

        int pointsIndex = Array.IndexOf(_fullCounts, (short)fullCount);
        if (pointsIndex < 0) return null;

        if (progress.IsLovedFeeded)
        {
            if (rewardGroup.Results.Count == 1) return rewardGroup.Results[0];

            var validRewards = new List<PetFeedResult>();
            int maxLevel = 0;
            foreach (var result in rewardGroup.Results)
            {
                int resultLevel = itemData.GetTemplate(result.Item)?.Level ?? 0;
                if (resultLevel > playerLevel) continue;
                if (resultLevel > maxLevel)
                {
                    maxLevel = resultLevel;
                    validRewards.Clear();
                }
                validRewards.Add(result);
            }
            if (validRewards.Count == 0) return null;
            return validRewards.Count == 1 ? validRewards[0] : validRewards[Random.Shared.Next(validRewards.Count)];
        }

        int rewardIndex = 0;
        int totalRewards = rewardGroup.Results.Count;
        for (int row = 1; row < _pointValues.Length; row++)
        {
            var points = _pointValues[row];
            if (points[pointsIndex] <= progress.TotalPoints)
                rewardIndex = (int)Math.Round((float)totalRewards / (_pointValues.Length - 1) * row) - 1;
        }

        if (rewardIndex < 0) rewardIndex = 0;
        else if (rewardIndex > rewardGroup.Results.Count - 1) rewardIndex = rewardGroup.Results.Count - 1;

        return rewardGroup.Results[rewardIndex];
    }
}

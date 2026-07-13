namespace AionLightning.Game.Model.Pet;

/// <summary>
/// Persisted + session state for one adopted toy pet (Java model.gameobjects.player.PetCommonData).
/// One instance per player_pets row; lives in the owning Player's <see cref="PetList"/> for the whole
/// session, independent of whether the pet is currently spawned in the world.
/// </summary>
public sealed class PetCommonData
{
    public int PetId { get; set; }
    public int Decoration { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Birthday { get; set; }
    public int MasterObjectId { get; set; }

    /// <summary>World object id assigned via ObjectIdFactory.Next() when the pet is spawned; 0 while despawned.</summary>
    public int PetObjectId { get; set; }

    /// <summary>Absolute time this pet's adoption expires (rental/event eggs); null = never expires.</summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>Last time the pet was despawned/parked; PetList uses this to pick the "last used" pet on login.</summary>
    public DateTime? DespawnTime { get; set; }

    // Feed/mood/doping raw columns — persisted to player_pets; see Hydrate/PersistFeedProgress and
    // the mood helpers below for how the feed/mood minigames (P2) read and mutate them.
    public int HungryLevel { get; set; }
    public int FeedProgress { get; set; }
    public long ReuseTime { get; set; }
    public string Dopings { get; set; } = string.Empty;
    public long MoodStarted { get; set; }
    public int Counter { get; set; }
    public long MoodCdStarted { get; set; }
    public long GiftCdStarted { get; set; }

    // Session-only (not persisted) — Java PetCommonData.cancelFeed/feedingTime/lastSentPoints.
    /// <summary>Set while an in-flight scheduled feed step should stop (Java <c>cancelFeed</c>).</summary>
    public bool CancelFeed { get; set; }
    /// <summary>False while the pet is on its post-full refeed cooldown (Java <c>feedingTime</c>).</summary>
    public bool IsFeedingTime { get; set; } = true;
    /// <summary>Last mood-points value sent to the client, for delta-only status updates (Java <c>lastSentPoints</c>).</summary>
    public int LastSentPoints { get; set; }

    /// <summary>Hydrates a live <see cref="PetFeedProgress"/> from the persisted packed <see cref="FeedProgress"/>
    /// int + <see cref="HungryLevel"/> (Java: DAO row hydration in <c>MySQL5PlayerPetsDAO.loadPets</c>).</summary>
    public PetFeedProgress HydrateFeedProgress(int lovedFoodLimit)
    {
        var progress = new PetFeedProgress((short)lovedFoodLimit) { HungryLevel = (PetHungryLevel)HungryLevel };
        progress.SetData(FeedProgress);
        return progress;
    }

    /// <summary>Flushes a live <see cref="PetFeedProgress"/> back into the persisted raw columns
    /// (Java: <c>PetSpawnService.dismissPet</c> → <c>PlayerPetsDAO.saveFeedStatus</c>).</summary>
    public void PersistFeedProgress(PetFeedProgress progress)
    {
        HungryLevel = (int)progress.HungryLevel;
        FeedProgress = progress.GetDataForPacket();
    }

    /// <summary>Java PetCommonData.getMoodPoints — elapsed-time-based mood accrual plus shuggle bonus.</summary>
    public int GetMoodPoints(bool forPacket)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (MoodStarted == 0) MoodStarted = now;
        int points = (int)Math.Round((now - MoodStarted) / 1000f) + Counter * 1000;
        if (forPacket && points > 9000) return 9000;
        return points;
    }

    /// <summary>Java PetCommonData.increaseShuggleCounter — gated by the 10-minute shuggle cooldown.</summary>
    public bool IncreaseShuggleCounter()
    {
        if (GetMoodRemainingTime() > 0) return false;
        MoodCdStarted = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Counter++;
        return true;
    }

    /// <summary>Java PetCommonData.clearMoodStatistics.</summary>
    public void ClearMoodStatistics()
    {
        MoodStarted = 0;
        Counter = 0;
    }

    /// <summary>Java PetCommonData.getMoodRemainingTime — seconds left on the 10-minute shuggle cooldown.</summary>
    public int GetMoodRemainingTime()
    {
        long stop = MoodCdStarted + 600_000;
        long remains = stop - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (remains <= 0) { MoodCdStarted = 0; return 0; }
        return (int)(remains / 1000);
    }

    /// <summary>Java PetCommonData.getGiftRemainingTime — seconds left on the 1-hour gift cooldown.</summary>
    public int GetGiftRemainingTime()
    {
        long stop = GiftCdStarted + 3_600_000;
        long remains = stop - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (remains <= 0) { GiftCdStarted = 0; return 0; }
        return (int)(remains / 1000);
    }

    /// <summary>Java PetCommonData.getRefeedDelay — remaining ms until the post-full refeed cooldown ends.</summary>
    public long GetRefeedDelayMs()
    {
        long time = ReuseTime - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (time < 0) { ReuseTime = 0; time = 0; }
        return time;
    }
}

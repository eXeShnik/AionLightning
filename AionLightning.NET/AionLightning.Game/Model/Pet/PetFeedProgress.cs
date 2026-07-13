namespace AionLightning.Game.Model.Pet;

/// <summary>
/// Live feed-minigame state for one spawned pet (Java <c>services/toypet/PetFeedProgress</c>).
/// Hydrated from <see cref="PetCommonData.HydrateFeedProgress"/> when the pet is spawned and flushed
/// back via <see cref="PetCommonData.PersistFeedProgress"/> on dismiss — see those two methods for the
/// bit-packed persistence format (mirrors Java's <c>getDataForPacket</c>/<c>setData</c>).
/// </summary>
public sealed class PetFeedProgress
{
    private int _totalPoints;
    private short _regularConsumed;
    private short _lovedConsumed;
    private readonly short _lovedFoodMax;

    public PetFeedProgress(short lovedFoodLimit) => _lovedFoodMax = (short)(lovedFoodLimit & 0x3F);

    public PetHungryLevel HungryLevel { get; set; } = PetHungryLevel.Hungry;

    public bool IsLovedFeeded { get; private set; }

    public int TotalPoints
    {
        get => _totalPoints;
        set => _totalPoints = value & 0x3FFF;
    }

    public int RegularCount => _regularConsumed & 0xFF;

    public int LovedFoodRemaining => _lovedFoodMax - _lovedConsumed;

    public void SetIsLovedFeeded() => IsLovedFeeded = true;

    public void IncrementCount(bool lovedFood)
    {
        if (lovedFood) _lovedConsumed++;
        else _regularConsumed++;
    }

    public void Reset()
    {
        if (IsLovedFeeded)
        {
            IsLovedFeeded = false;
        }
        else
        {
            _totalPoints = 0;
            _regularConsumed = 0;
        }
    }

    /// <summary>Packs regularCount(8b) &lt;&lt; totalPoints/4(12b) &lt;&lt; lovedConsumed(6b) &lt;&lt; unk(4b).</summary>
    public int GetDataForPacket()
    {
        int value = RegularCount & 0xFF;
        value <<= 14;
        value |= _totalPoints >> 2;
        value <<= 6;
        value |= _lovedConsumed & 0x3F;
        value <<= 4; // unk
        return value;
    }

    public void SetData(int savedData)
    {
        savedData >>= 4; // drop unk
        _lovedConsumed = (short)(savedData & 0x3F);
        savedData >>= 6;
        _totalPoints = (savedData & 0x3FFF) << 2;
        savedData >>= 14;
        _regularConsumed = (short)(savedData & 0xFF);
    }
}

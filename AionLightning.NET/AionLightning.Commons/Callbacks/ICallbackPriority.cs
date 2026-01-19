namespace AionLightning.Commons.Callbacks
{
    public interface ICallbackPriority
    {
        const int DefaultPriority = 0;
        int Priority { get; }
    }
}
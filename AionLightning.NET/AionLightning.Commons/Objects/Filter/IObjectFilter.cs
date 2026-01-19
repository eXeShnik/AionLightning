namespace AionLightning.Commons.Objects.Filter
{
    public interface IObjectFilter<T>
    {
        bool AcceptObject(T obj);
    }
}

namespace AionLightning.Commons.Callbacks
{
    public interface ICallback<T>
    {
        void Invoke(object[] args);
        CallbackResult BeforeCall(T obj, object[] args);
        CallbackResult AfterCall(T obj, object[] args, object methodResult);
        Type GetBaseClass();
    }
}
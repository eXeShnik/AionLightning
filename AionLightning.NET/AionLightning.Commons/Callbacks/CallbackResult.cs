namespace AionLightning.Commons.Callbacks
{
    public class CallbackResult
    {
        public static readonly CallbackResult PASSED = new CallbackResult(0);
        public static readonly CallbackResult FAILED = new CallbackResult(1);

        public int Result { get; }
        public object ResultObject { get; }
        public bool Continue { get; }

        public CallbackResult(bool shouldContinue)
        {
            Continue = shouldContinue;
            Result = 0;
            ResultObject = null;
        }

        public CallbackResult(int result)
        {
            Result = result;
            ResultObject = null;
        }

        public CallbackResult(object resultObject, int result)
        {
            ResultObject = resultObject;
            Result = result;
            Continue = true;
        }

        public T GetResultObject<T>()
        {
            return (T)ResultObject;
        }

        public bool IsBlockingCallbacks()
        {
            return !Continue;
        }
    }
}
using System.Collections.Generic;
using System.Threading;

namespace AionLightning.Commons.Callbacks
{
    public interface IEnhancedObject
    {
        void AddCallback(ICallback<object> callback);
        void RemoveCallback(ICallback<object> callback);
        IDictionary<Type, IList<ICallback<object>>> Callbacks { get; set; }
        ReaderWriterLockSlim CallbackLock { get; }
    }
}
using System.Collections.Generic;

namespace AionLightning.Commons.Utils
{
    public class AEFastSet<T> : HashSet<T>
    {
        public AEFastSet() : base()
        {
        }

        public AEFastSet(int capacity) : base(capacity)
        {
        }

        public AEFastSet(IEnumerable<T> collection) : base(collection)
        {
        }
    }
}

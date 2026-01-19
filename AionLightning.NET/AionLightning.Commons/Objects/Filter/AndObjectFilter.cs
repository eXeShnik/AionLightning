namespace AionLightning.Commons.Objects.Filter
{
    public class AndObjectFilter<T> : IObjectFilter<T>
    {
        private readonly IObjectFilter<T>[] _filters;

        public AndObjectFilter(params IObjectFilter<T>[] filters)
        {
            _filters = filters;
        }

        public bool AcceptObject(T obj)
        {
            foreach (var filter in _filters)
            {
                if (filter != null && !filter.AcceptObject(obj))
                    return false;
            }
            return true;
        }
    }
}

using System;

namespace LaunchDarkly.Cache
{
    internal sealed class SingleValueCacheImpl<V> : ISingleValueCache<V>
    {
        private readonly ICache<object, V> _cache;
        
        public SingleValueCacheImpl(SingleValueCacheBuilder<V> builder)
        {
            Func<V> loaderFn = builder.LoaderFn;
            Func<object, V> cacheLoaderFn = (object o) => loaderFn();
            _cache = Caches.KeyValue<object, V>()
                .WithLoader(cacheLoaderFn)
                .WithInitialCapacity(1)
                .WithMaximumEntries(1)
                .WithExpiration(builder.Expiration)
                .WithBackgroundPurge(builder.PurgeInterval)
                .Build();
        }

        public bool HasValue
        {
            get
            {
                return _cache.ContainsKey(this);
            }
        }

        public V Get()
        {
            return _cache.Get(this);
        }

        public bool TryGetValue(out V value)
        {
            return _cache.TryGetValue(this, out value);
        }

        public void Set(V value)
        {
            _cache.Set(this, value);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}

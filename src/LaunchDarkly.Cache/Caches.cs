using System;

namespace LaunchDarkly.Cache
{
    /// <summary>
    /// Methods for building caches.
    /// </summary>
    public abstract class Caches
    {
        /// <summary>
        /// Starts constructing a key-value cache.
        /// </summary>
        /// <typeparam name="K">the key type</typeparam>
        /// <typeparam name="V">the value type</typeparam>
        /// <returns>a builder</returns>
        public static CacheBuilder<K, V> KeyValue<K, V>()
        {
            return new CacheBuilder<K, V>();
        }

        /// <summary>
        /// Starts constructing a cache that contains only a single value, with no key.
        /// </summary>
        /// <typeparam name="V">the value type</typeparam>
        /// <returns>a builder</returns>
        public static SingleValueCacheBuilder<V> SingleValue<V>()
        {
            return new SingleValueCacheBuilder<V>();
        }
    }

    /// <summary>
    /// Basic builder methods common to all caches.
    /// </summary>
    /// <typeparam name="B">the specific builder subclass</typeparam>
    public class CacheBuilderBase<B> where B : CacheBuilderBase<B>
    {
        internal TimeSpan? Expiration { get; private set; }
        internal TimeSpan? PurgeInterval { get; private set; }
        
        /// <summary>
        /// Sets the maximum time (TTL) that any value will be retained in the cache. This time is
        /// counted from the time when the value was last written (added or updated).
        /// 
        /// If this is null, values will never expire.
        /// </summary>
        /// <param name="expiration">the expiration time, or null if values should never expire</param>
        /// <returns></returns>
        public B WithExpiration(TimeSpan? expiration)
        {
            Expiration = expiration;
            return (B)this;
        }

        /// <summary>
        /// Sets the interval in between automatic purges of expired values.
        /// 
        /// If this is not null, then a background task will run at that frequency to sweep the cache for
        /// all expired values.
        /// 
        /// If it is null, expired values will be removed only at the time when you try to access them.
        /// 
        /// This value is ignored if the expiration time (<see cref="WithExpiration(TimeSpan?)"/>) is null.
        /// </summary>
        /// <param name="purgeInterval">the purge interval, or null to turn off automatic purging</param>
        /// <returns></returns>
        public B WithBackgroundPurge(TimeSpan? purgeInterval)
        {
            PurgeInterval = purgeInterval;
            return (B)this;
        }
    }

    /// <summary>
    /// Builder for a key-value cache.
    /// </summary>
    /// <typeparam name="K">the key type</typeparam>
    /// <typeparam name="V">the value type</typeparam>
    /// <see cref="Caches.KeyValue{K, V}"/>
    public class CacheBuilder<K, V> : CacheBuilderBase<CacheBuilder<K, V>>
    {
        internal Func<K, V> LoaderFn { get; private set; }
        internal int? InitialCapacity { get; private set; }
        internal int? MaximumEntries { get; private set; }

        /// <summary>
        /// Specifies a value computation function for a read-through cache.
        /// 
        /// If this is not null, then any call to <see cref="ICache{K, V}.Get(K)"/> or
        /// <see cref="ICache{K, V}.TryGetValue(K, out V)"/> with a key that is not already in the
        /// cache will cause the function to be called with that key as an argument; the returned
        /// value will then be stored in the cache and returned to the caller.
        /// 
        /// If the function is null (the default), then the cache will not be a read-through cache
        /// and will only provide values that were explicitly set.
        /// </summary>
        /// <param name="loaderFn">the function for generating values</param>
        /// <returns>the builder</returns>
        public CacheBuilder<K, V> WithLoader(Func<K, V> loaderFn)
        {
            LoaderFn = loaderFn;
            return this;
        }

        /// <summary>
        /// Specifies the initial capacity of the cache.
        /// 
        /// This is the same as the optional constructor parameter for <code>Dictionary</code>.
        /// It does not affect how many entries can be stored, only how soon the backing
        /// dictionary will need to be resized.
        /// </summary>
        /// <param name="initialCapacity">the initial capacity, or null to use the default</param>
        /// <returns>the builder</returns>
        public CacheBuilder<K, V> WithInitialCapacity(int? initialCapacity)
        {
            if (initialCapacity != null && initialCapacity.Value < 0)
            {
                throw new ArgumentException("must be >= 0 if not null", nameof(initialCapacity));
            }
            InitialCapacity = initialCapacity;
            return this;
        }

        /// <summary>
        /// Specifies the maximum number of entries that can be in the cache.
        /// 
        /// If this is not null, then any attempt to add more entries when the cache has reached
        /// this limit will result in existing entries being evicted, in the order that they were
        /// originally added or last updated.
        /// 
        /// If it is null (the default), then there is no such limit.
        /// </summary>
        /// <param name="maximumEntries">the maximum capacity, or null for no limit</param>
        /// <returns>the builder</returns>
        public CacheBuilder<K, V> WithMaximumEntries(int? maximumEntries)
        {
            if (maximumEntries != null && maximumEntries.Value <= 0)
            {
                throw new ArgumentException("must be > 0 if not null", nameof(maximumEntries));
            }
            MaximumEntries = maximumEntries;
            return this;
        }

        /// <summary>
        /// Constructs a cache with the specified properties.
        /// </summary>
        /// <returns>a cache instance</returns>
        public ICache<K, V> Build()
        {
            return new CacheImpl<K, V>(this);
        }
    }

    /// <summary>
    /// Builder for a single-value cache.
    /// </summary>
    /// <typeparam name="V">the value type</typeparam>
    public class SingleValueCacheBuilder<V> : CacheBuilderBase<SingleValueCacheBuilder<V>>
    {
        internal Func<V> LoaderFn { get; private set; }

        /// <summary>
        /// Specifies a value computation function for a read-through cache.
        /// 
        /// If this is not null, then any call to <see cref="ISingleValueCache{V}.Get"/> or
        /// <see cref="ISingleValueCache{V}.TryGetValue(out V)"/> with a key that is not already in the
        /// cache will cause the function to be called; the returned value will then be stored in
        /// the cache and returned to the caller.
        /// 
        /// If the function is null (the default), then the cache will not be a read-through cache
        /// and will only provide values that were explicitly set.
        /// </summary>
        /// <param name="loaderFn">the function for generating values</param>
        /// <returns>the builder</returns>
        public SingleValueCacheBuilder<V> WithLoader(Func<V> loaderFn)
        {
            LoaderFn = loaderFn;
            return this;
        }

        /// <summary>
        /// Constructs a cache with the specified properties.
        /// </summary>
        /// <returns>a cache instance</returns>
        public ISingleValueCache<V> Build()
        {
            return new SingleValueCacheImpl<V>(this);
        }
    }
}

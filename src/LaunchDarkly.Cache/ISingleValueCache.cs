using System;

namespace LaunchDarkly.Cache
{
    /// <summary>
    /// Interface for a cache that holds a single value, with no key.
    /// </summary>
    /// <typeparam name="V">the value type</typeparam>
    public interface ISingleValueCache<V> : IDisposable
    {
        /// <summary>
        /// Returns true if the cache contains a value.
        /// 
        /// In a read-through cache, this property will always be true, since calling
        /// <see cref="Get"/> will always call the loader function to acquire a value if the
        /// value was not already cached.
        /// </summary>
        /// <returns>true if there is a value</returns>
        bool HasValue { get; }

        /// <summary>
        /// Attempts to get the current value.
        /// 
        /// In a read-through cache, if there is no cached value for, the cache will call the loader
        /// function to provide a value; thus, a value is always available.
        ///
        /// If no value is available, the cache does not throw an exception. Instead, it returns the
        /// default value for type V (i.e. null if it is a reference type). Note that any value
        /// (including null, for reference types) can be cached, so if you need to distinguish
        /// between the lack of a value and a default value you must use
        /// <see cref="HasValue"/> or <see cref="TryGetValue(out V)"/>.
        /// </summary>
        /// <returns>the current value, or <code>default(V)</code></returns>
        V Get();

        /// <summary>
        /// Attempts to get the current value. If successful, sets <code>value</code> to the value
        /// and returns true; otherwise, sets <code>value</code> to <code>default(V)</code> and
        /// returns false.
        /// 
        /// This is the same as calling <see cref="HasValue"/> followed by <see cref="Get"/>
        /// except that it is an atomic operation.
        /// </summary>
        /// <returns>the current value, or <code>default(V)</code></returns>
        bool TryGetValue(out V value);

        /// <summary>
        /// Stores a value.
        /// 
        /// Note that any value of type V can be cached, including null for reference types.
        /// </summary>
        /// <param name="value">the value</param>
        void Set(V value);
        
        /// <summary>
        /// Removes the cached value, if any.
        /// </summary>
        void Clear();
    }
}

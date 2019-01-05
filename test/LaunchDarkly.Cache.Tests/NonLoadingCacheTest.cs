using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaunchDarkly.Cache.Tests
{
    public class NonLoadingCacheTest
    {
        [Fact]
        public void GetMissingValue()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            Assert.Null(cache.Get("key"));
        }

        [Fact]
        public void GetMissingIntValue()
        {
            var cache = Caches.KeyValue<string, int>().Build();
            Assert.Equal(0, cache.Get("key"));
        }

        [Fact]
        public void GetCachedValue()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            cache.Set("key", "value");
            Assert.Equal("value", cache.Get("key"));
        }

        [Fact]
        public void ContainsKeyIsFalseForUnknownKey()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            Assert.False(cache.ContainsKey("key"));
        }

        [Fact]
        public void ContainsKeyIsTrueForKnownKey()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            cache.Set("key", "value");
            Assert.True(cache.ContainsKey("key"));
        }

        [Fact]
        public void TryGetValueReturnsFalseForUnknownKey()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            var found = cache.TryGetValue("key", out var value);
            Assert.False(found);
            Assert.Null(value);
        }

        [Fact]
        public void TryGetValueReturnsTrueForKnownKey()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            cache.Set("key", "value");
            var found = cache.TryGetValue("key", out var value);
            Assert.True(found);
            Assert.Equal("value", value);
        }

        [Fact]
        public void RemoveValue()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            cache.Set("key", "value");
            cache.Remove("key");
            Assert.Null(cache.Get("key"));
        }

        [Fact]
        public void RemoveAllValues()
        {
            var cache = Caches.KeyValue<string, string>().Build();
            cache.Set("foo", "value1");
            cache.Set("bar", "value2");
            cache.Clear();
            Assert.Null(cache.Get("foo")); // value was recomputed
            Assert.Null(cache.Get("bar")); // value was recomputed
        }

        [Fact]
        public void ValueCanExpire()
        {
            using (var cache = Caches.KeyValue<string, string>()
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(25))
                    .Build())
            {
                cache.Set("key", "value");
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Null(cache.Get("key"));
            }
        }

        [Fact]
        public void ComputedValueCanExpireEvenIfPurgeTaskHasNotRunYet()
        {
            using (var cache = Caches.KeyValue<string, string>()
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(500))
                    .Build())
            {
                cache.Set("key", "value");
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Null(cache.Get("key"));
            }
        }

        [Fact]
        public void ComputedValueCanExpireEvenIfThereIsNoPurgeTask()
        {
            using (var cache = Caches.KeyValue<string, string>()
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(null)
                    .Build())
            {
                cache.Set("key", "value");
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Null(cache.Get("key"));
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LaunchDarkly.Cache.Tests
{
    public class LoadingCacheTest
    {
        private TestValueGenerator valueGenerator = new TestValueGenerator();

        [Fact]
        public void GetNewlyComputedValue()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            Assert.Equal("key_value_1", cache.Get("key"));
        }

        [Fact]
        public void GetCachedValue()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            Assert.Equal("key_value_1", cache.Get("key"));
            Assert.Equal("key_value_1", cache.Get("key")); // value was not recomputed
            Assert.Equal(1, valueGenerator.TimesCalled);
        }

        [Fact]
        public void GetExplicitlySetValue()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            cache.Set("key", "other");
            Assert.Equal("other", cache.Get("key"));
        }

        [Fact]
        public void ContainsKeyIsTrueForUnknownKey()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            Assert.True(cache.ContainsKey("key"));
        }

        [Fact]
        public void ContainsKeyIsTrueForKnownKey()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            cache.Set("key", "value");
            Assert.True(cache.ContainsKey("key"));
        }

        [Fact]
        public void TryGetValueReturnsTrueForUnknownKey()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            var found = cache.TryGetValue("key", out var value);
            Assert.True(found);
            Assert.Equal("key_value_1", value);
        }

        [Fact]
        public void TryGetValueReturnsTrueForKnownKey()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            cache.Set("key", "value");
            var found = cache.TryGetValue("key", out var value);
            Assert.True(found);
            Assert.Equal("value", value);
        }

        [Fact]
        public void RemoveValue()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            Assert.Equal("foo_value_1", cache.Get("foo"));
            Assert.Equal("bar_value_2", cache.Get("bar"));
            cache.Remove("foo");
            Assert.Equal("foo_value_3", cache.Get("foo")); // value was recomputed
            Assert.Equal("bar_value_2", cache.Get("bar"));
        }

        [Fact]
        public void RemoveAllValues()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            Assert.Equal("foo_value_1", cache.Get("foo"));
            Assert.Equal("bar_value_2", cache.Get("bar"));
            cache.Clear();
            Assert.Equal("foo_value_3", cache.Get("foo")); // value was recomputed
            Assert.Equal("bar_value_4", cache.Get("bar")); // value was recomputed
        }

        [Fact]
        public void ComputedValueCanExpire()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(25))
                    .Build())
            {
                Assert.Equal("key_value_1", cache.Get("key"));
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Equal("key_value_2", cache.Get("key"));
            }
        }

        [Fact]
        public void ComputedValueCanExpireEvenIfPurgeTaskHasNotRunYet()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(500))
                    .Build())
            {
                Assert.Equal("key_value_1", cache.Get("key"));
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Equal("key_value_2", cache.Get("key"));
            }
        }

        [Fact]
        public void ComputedValueCanExpireEvenIfThereIsNoPurgeTask()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .Build())
            {
                Assert.Equal("key_value_1", cache.Get("key"));
                Thread.Sleep(TimeSpan.FromMilliseconds(150));
                Assert.Equal("key_value_2", cache.Get("key"));
            }
        }

        [Fact]
        public void ExplicitlySetValueCanExpire()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithExpiration(TimeSpan.FromMilliseconds(200))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(25))
                    .Build())
            {
                cache.Set("key", "other");
                Thread.Sleep(TimeSpan.FromMilliseconds(250));
                Assert.Equal("key_value_1", cache.Get("key"));
            }
        }

        [Fact]
        public void OldestValueIsEvictedAfterMaximumIsReached()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithMaximumEntries(2)
                    .Build())
            {
                Assert.Equal("a_value_1", cache.Get("a"));
                Assert.Equal("b_value_2", cache.Get("b"));
                Assert.Equal("c_value_3", cache.Get("c"));
                Assert.Equal("d_value_4", cache.Get("d"));

                Assert.Equal("c_value_3", cache.Get("c")); // not recomputed
                Assert.Equal("d_value_4", cache.Get("d")); // not recomputed

                Assert.Equal("a_value_5", cache.Get("a")); // recomputed
                Assert.Equal("b_value_6", cache.Get("b")); // recomputed
            }
        }

        [Fact]
        public void MultipleRequestsForNewValueAreCoalesced()
        {
            var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue).Build();
            valueGenerator.Delay = TimeSpan.FromMilliseconds(200);
            var tasks = new Task[3];
            for (var i = 0; i < 3; i++)
            {
                tasks[i] = Task.Run(() => cache.Get("key"));
            }
            Task.WaitAll(tasks);
            Assert.Equal(1, valueGenerator.TimesCalled);
        }

        [Fact]
        public void RequestsAreCoalescedWhenReplacingExpiredValue()
        {
            using (var cache = Caches.KeyValue<string, string>().WithLoader(valueGenerator.GetNextValue)
                    .WithExpiration(TimeSpan.FromMilliseconds(100))
                    .WithBackgroundPurge(TimeSpan.FromMilliseconds(500))
                    .Build())
            {
                cache.Set("key", "old");
                Thread.Sleep(110);
                var tasks = new Task[3];
                for (var i = 0; i < 3; i++)
                {
                    tasks[i] = Task.Run(() => cache.Get("key"));
                }
                Task.WaitAll(tasks);
                Assert.Equal(1, valueGenerator.TimesCalled);
            }
        }

        private class TestValueGenerator
        {
            public volatile int TimesCalled = 0;
            public TimeSpan? Delay = null;

            public String GetNextValue(string key)
            {
                int n = Interlocked.Increment(ref TimesCalled);
                if (Delay != null)
                {
                    Thread.Sleep(Delay.Value);
                }
                return key + "_value_" + n;
            }
        }
    }
}

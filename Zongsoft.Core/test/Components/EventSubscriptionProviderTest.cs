using System;
using System.Threading;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests
{
	public class EventSubscriptionProviderTest
	{
		private readonly EventRegistry _registry = new();

		[Fact]
		public void TestGetSubscriptions()
		{
			Assert.Throws<NotImplementedException>(TestWeakly);
			Assert.Throws<NotImplementedException>(TestStrong);

			void TestWeakly()
			{
				object weaklyProvider1 = new WeaklyEventSubscriptionProvider1();
				EventSubscriptionProviderUtility.GetSubscriptionsAsync(weaklyProvider1, new EventContext(_registry, "EventName", "This is a event argument."), default);

				object weaklyProvider2 = new WeaklyEventSubscriptionProvider2();
				EventSubscriptionProviderUtility.GetSubscriptionsAsync(weaklyProvider2, new EventContext(_registry, "EventName", "This is a event argument."), default);
			}

			void TestStrong()
			{
				object strongProvider1 = new StrongEventSubscriptionProvider1();
				EventSubscriptionProviderUtility.GetSubscriptionsAsync(strongProvider1, new EventContext<MyArgument>(_registry, "EventName", new MyArgument("MyName", "MyCode")), default);

				object strongProvider2 = new StrongEventSubscriptionProvider2();
				EventSubscriptionProviderUtility.GetSubscriptionsAsync(strongProvider2, new EventContext<MyArgument>(_registry, "EventName", new MyArgument("MyName", "MyCode")), default);
			}
		}

		public class EventRegistry : EventRegistryBase
		{
			public EventRegistry() : base("Useless") { }
		}

		public class WeaklyEventSubscriptionProvider1 : IEventSubscriptionProvider
		{
			public IAsyncEnumerable<IEventSubscription> GetSubscriptionsAsync(EventContextBase context, CancellationToken cancellation)
			{
				throw new NotImplementedException();
			}
		}

		public class WeaklyEventSubscriptionProvider2 : IEventSubscriptionProvider
		{
			IAsyncEnumerable<IEventSubscription> IEventSubscriptionProvider.GetSubscriptionsAsync(EventContextBase context, CancellationToken cancellation)
			{
				throw new NotImplementedException();
			}
		}

		public class StrongEventSubscriptionProvider1 : IEventSubscriptionProvider<MyArgument>
		{
			public IAsyncEnumerable<IEventSubscription> GetSubscriptionsAsync(EventContext<MyArgument> context, CancellationToken cancellation)
			{
				throw new NotImplementedException();
			}
		}

		public class StrongEventSubscriptionProvider2 : IEventSubscriptionProvider<MyArgument>
		{
			IAsyncEnumerable<IEventSubscription> IEventSubscriptionProvider<MyArgument>.GetSubscriptionsAsync(EventContext<MyArgument> context, CancellationToken cancellation)
			{
				throw new NotImplementedException();
			}
		}

		public struct MyArgument
		{
			public MyArgument(string name, string code)
			{
				this.Name = name;
				this.Code = code;
			}
			public string Name { get; set; }
			public string Code { get; set; }
		}
	}
}

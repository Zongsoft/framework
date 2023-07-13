using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;

using Xunit;

namespace Zongsoft.Components.Tests
{
	public class EventsTest
	{
		private readonly App _app;

        public EventsTest()
		{
			_app = new App(new ServiceProviderFactory());
			_app.Modules.Add(ModuleA.Current);
			_app.Modules.Add(ModuleB.Current);
		}

        [Fact]
		public void TestGetEvents()
		{
			var descriptors = Events.GetEvents();

			Assert.NotNull(descriptors);
			Assert.NotEmpty(descriptors);
		}

		[Fact]
		public void TestGetEvent()
		{
			var descriptor = Events.GetEvent("");
			Assert.Null(descriptor);

			descriptor = Events.GetEvent("Created");
			Assert.Null(descriptor);

			descriptor = Events.GetEvent("ModuleB:Target1.Created");
			Assert.NotNull(descriptor);

			descriptor = Events.GetEvent("ModuleB:Target1.StatusChanged");
			Assert.NotNull(descriptor);
		}
	}

	public class App : ApplicationContext
	{
		public App(ServiceProviderFactory factory) : base(factory.CreateServiceProvider(new ServiceCollection())) { }
	}

	public class ModuleA : ApplicationModule
	{
		public static readonly ModuleA Current = new();

		private ModuleA() : base(nameof(ModuleA)) { }
	}

	public class ModuleB : ApplicationModule<ModuleB.EventRegistry>
	{
		public static readonly ModuleB Current = new();

		private ModuleB() : base(nameof(ModuleB)) { }

		public sealed class EventRegistry : EventRegistryBase
		{
			public EventRegistry() : base(nameof(ModuleB))
			{
				this.Event(Target1Event.Created);
				this.Event(Target1Event.StatusChanged);
				this.Event(Target2Event.Created);
				this.Event(Target2Event.StatusChanged);

				this.Target1 = new Target1Event(this);
				this.Target2 = new Target2Event(this);
			}

			public Target1Event Target1 { get; }
			public Target2Event Target2 { get; }

			public sealed class Target1Event
			{
				#region 静态字段
				internal static readonly EventDescriptor Created = new($"Target1.{nameof(Created)}");
				internal static readonly EventDescriptor StatusChanged = new($"Target1.{nameof(StatusChanged)}");
				#endregion

				#region 成员字段
				private readonly EventRegistry _registry;
				#endregion

				#region 构造函数
				internal Target1Event(EventRegistry registry) => _registry = registry;
				#endregion
			}

			public sealed class Target2Event
			{
				#region 静态字段
				internal static readonly EventDescriptor Created = new($"Target2.{nameof(Created)}");
				internal static readonly EventDescriptor StatusChanged = new($"Target2.{nameof(StatusChanged)}");
				#endregion

				#region 成员字段
				private readonly EventRegistry _registry;
				#endregion

				#region 构造函数
				internal Target2Event(EventRegistry registry) => _registry = registry;
				#endregion
			}
		}
	}
}
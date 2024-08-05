using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;

using Xunit;
using System.Threading.Tasks;
using System.Threading;

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
			var descriptor = Events.GetEvent(null);
			Assert.Null(descriptor); //查找失败

			descriptor = Events.GetEvent("Created");
			Assert.Null(descriptor); //查找失败

			descriptor = Events.GetEvent("ModuleB:Target1.Created");
			Assert.NotNull(descriptor);
			Assert.Same(ModuleB.EventRegistry.Target1Event.Created, descriptor);
			Assert.Equal("Target1.Created", descriptor.Name);
			Assert.Equal(Properties.Resources.Target1_Created, descriptor.Title);
			Assert.Equal(Properties.Resources.Target1_Created_Description, descriptor.Description);

			descriptor = Events.GetEvent("ModuleB:Target1.StatusChanged");
			Assert.NotNull(descriptor);
			Assert.Same(ModuleB.EventRegistry.Target1Event.StatusChanged, descriptor);
			Assert.Equal("Target1.StatusChanged", descriptor.Name);
			Assert.Equal(Properties.Resources.Target1_StatusChanged, descriptor.Title);
			Assert.Equal(Properties.Resources.Target1_StatusChanged_Description, descriptor.Description);
		}

		[Fact]
		public void TestRaiseEvent()
		{
			var createdHandler = new CreatedHandler();
			var descriptor = Events.GetEvent("ModuleB:Target1.Created");
			Assert.NotNull(descriptor);
			descriptor.Handlers.Add(createdHandler);
			Assert.Contains(createdHandler, descriptor.Handlers);
			Assert.Throws<NotImplementedException>(OnCreate);
			descriptor.Handlers.Remove(createdHandler);
			Assert.DoesNotContain(createdHandler, descriptor.Handlers);

			var changedHandler = new StatusChangedHandler();
			descriptor = Events.GetEvent("ModuleB:Target1.StatusChanged");
			Assert.NotNull(descriptor);
			descriptor.Handlers.Add(changedHandler);
			Assert.Contains(changedHandler, descriptor.Handlers);
			Assert.Throws<NotImplementedException>(OnStatusChanged);
			descriptor.Handlers.Remove(changedHandler);
			Assert.DoesNotContain(changedHandler, descriptor.Handlers);

			static void OnCreate() => ModuleB.Current.Events.Target1.OnCreated(new("MyMessage"), new Dictionary<string, object> { { "MyInteger", 100 } });
			static void OnStatusChanged() => ModuleB.Current.Events.Target1.OnStatusChanged(new(1, "MyStatusChanged"), new Dictionary<string, object> { { "MyInteger", 200 } });
		}

		[Fact]
		public void TestUnmarshalEvent()
		{
			var json = @"{""Argument"":{""Message"":""MyMessage""},""Parameters"":{""MyInteger"":100}}";
			var data = System.Text.Encoding.UTF8.GetBytes(json);

			(var argument, var parameters) = Events.Marshaler.Unmarshal("ModuleB:Target1.Created", data);

			Assert.NotNull(argument);
			Assert.IsType<CreatedArgument>(argument);

			Assert.NotNull(parameters);
			Assert.NotEmpty(parameters);
		}

		private class CreatedHandler : HandlerBase<CreatedArgument>
		{
			protected override ValueTask OnHandleAsync(object caller, CreatedArgument argument, IDictionary<string, object> parameters, CancellationToken cancellation) => throw new NotImplementedException();
		}

		private class StatusChangedHandler : HandlerBase<StatusChangedArgument>
		{
			protected override ValueTask OnHandleAsync(object caller, StatusChangedArgument argument, IDictionary<string, object> parameters, CancellationToken cancellation) => throw new NotImplementedException();
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
				internal static readonly EventDescriptor<CreatedArgument> Created = new($"Target1.{nameof(Created)}");
				internal static readonly EventDescriptor<StatusChangedArgument> StatusChanged = new($"Target1.{nameof(StatusChanged)}");
				#endregion

				#region 成员字段
				private readonly EventRegistry _registry;
				#endregion

				#region 构造函数
				internal Target1Event(EventRegistry registry) => _registry = registry;
				#endregion

				#region 公共方法
				public void OnCreated(CreatedArgument argument, IDictionary<string, object> parameters = null) => _registry.Raise(Created.Name, argument, parameters);
				public void OnStatusChanged(StatusChangedArgument argument, IDictionary<string, object> parameters = null) => _registry.Raise(StatusChanged.Name, argument, parameters);
				#endregion
			}

			public sealed class Target2Event
			{
				#region 静态字段
				internal static readonly EventDescriptor<CreatedArgument> Created = new($"Target2.{nameof(Created)}");
				internal static readonly EventDescriptor<StatusChangedArgument> StatusChanged = new($"Target2.{nameof(StatusChanged)}");
				#endregion

				#region 成员字段
				private readonly EventRegistry _registry;
				#endregion

				#region 构造函数
				internal Target2Event(EventRegistry registry) => _registry = registry;
				#endregion

				#region 公共方法
				public void OnCreated(CreatedArgument argument, IDictionary<string, object> parameters = null) => _registry.Raise(Created.Name, argument, parameters);
				public void OnStatusChanged(StatusChangedArgument argument, IDictionary<string, object> parameters = null) => _registry.Raise(StatusChanged.Name, argument, parameters);
				#endregion
			}
		}
	}

	public class CreatedArgument
	{
		public CreatedArgument() { }
		public CreatedArgument(string message) => this.Message = message;

		public string Message { get; set; }
	}

	public class StatusChangedArgument
	{
		public StatusChangedArgument() { }
		public StatusChangedArgument(int status, string description = null)
		{
			this.Status = status;
			this.Timestamp = DateTime.Now;
			this.Description = description;
		}

		public int Status { get; set; }
		public DateTime Timestamp { get; set; }
		public string Description { get; set; }
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Collections;

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

			static void OnCreate() => ModuleB.Current.Events.Target1.OnCreated(new("MyMessage"), Parameters.Parameter("MyInteger", 100));
			static void OnStatusChanged() => ModuleB.Current.Events.Target1.OnStatusChanged(new(1, "MyStatusChanged"), Parameters.Parameter("MyInteger", 200));
		}

		[Fact]
		public void TestMarshalEvent()
		{
			const string EVENT_NAME = "Acquirer.Acquired";

			var meter = new Meter("Meter#1", new Metric("Metric.Key", "Metric.Code", 250.5));
			var context = new EventContext<Meter>(ModuleB.Current.Events, EVENT_NAME, meter);
			var data = Events.Marshaler.Marshal(context);

			(var argument, var parameters) = Events.Marshaler.Unmarshal($"{ModuleB.Current.Name}:{EVENT_NAME}", data);
			Assert.NotNull(argument);
			Assert.IsType<Meter>(argument);
			Assert.Equal("Meter#1", ((Meter)argument).Key);
			Assert.NotEmpty(((Meter)argument).Metrics);
			Assert.Equal("Metric.Key", ((Meter)argument).Metrics[0].Key);
			Assert.Equal("Metric.Code", ((Meter)argument).Metrics[0].Code);
			//Assert.Equal(250.5, ((Meter)argument).Metrics[0].Value);
		}

		[Fact]
		public void TestUnmarshalEvent()
		{
			var json = """
			{
				"Argument": {
					"Message": "MyMessage"
				},
				"Parameters":
				{
					"MyString": "StringValue",
					"MyInt32": {
						"$type": "int",
						"value": 100
					}
				}
			}
			""";

			var data = System.Text.Encoding.UTF8.GetBytes(json);

			(var argument, var parameters) = Events.Marshaler.Unmarshal("ModuleB:Target1.Created", data);

			Assert.NotNull(argument);
			Assert.IsType<CreatedArgument>(argument);
			Assert.Equal("MyMessage", ((CreatedArgument)argument).Message);

			Assert.NotNull(parameters);
			Assert.NotEmpty(parameters);

			Assert.True(parameters.TryGetValue("MyInt32", out var value));
			Assert.True(value is int);
			Assert.Equal(100, (int)value);

			Assert.True(parameters.TryGetValue("mystring", out value));
			Assert.True(value is string);
			Assert.Equal("StringValue", (string)value);
		}

		private class CreatedHandler : HandlerBase<CreatedArgument>
		{
			protected override ValueTask OnHandleAsync(CreatedArgument argument, Parameters parameters, CancellationToken cancellation) => throw new NotImplementedException();
		}

		private class StatusChangedHandler : HandlerBase<StatusChangedArgument>
		{
			protected override ValueTask OnHandleAsync(StatusChangedArgument argument, Parameters parameters, CancellationToken cancellation) => throw new NotImplementedException();
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
				this.Event(AcquirerEvent.Acquired);

				this.Target1 = new Target1Event(this);
				this.Target2 = new Target2Event(this);
				this.Acquirer = new AcquirerEvent(this);
			}

			public Target1Event Target1 { get; }
			public Target2Event Target2 { get; }
			public AcquirerEvent Acquirer { get; }

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
				public bool OnCreated(CreatedArgument argument, Parameters parameters = null) => _registry.Raise(Created.Name, argument, parameters);
				public bool OnStatusChanged(StatusChangedArgument argument, Parameters parameters = null) => _registry.Raise(StatusChanged.Name, argument, parameters);
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
				public bool OnCreated(CreatedArgument argument, Parameters parameters = null) => _registry.Raise(Created.Name, argument, parameters);
				public bool OnStatusChanged(StatusChangedArgument argument, Parameters parameters = null) => _registry.Raise(StatusChanged.Name, argument, parameters);
				#endregion
			}

			public sealed class AcquirerEvent
			{
				#region 静态字段
				internal static readonly EventDescriptor<Meter> Acquired = new($"Acquirer.{nameof(Acquired)}");
				#endregion

				#region 成员字段
				private readonly EventRegistry _registry;
				#endregion

				#region 构造函数
				internal AcquirerEvent(EventRegistry registry) => _registry = registry;
				#endregion

				#region 公共方法
				public bool OnAcquired(CreatedArgument argument, Parameters parameters = null) => _registry.Raise(Acquired.Name, argument, parameters);
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

	public partial struct Meter : IEquatable<Meter>
	{
		#region 构造函数
		public Meter(string key, params Metric[] metrics)
		{
			this.Key = key;
			this.Metrics = new MetricCollection(metrics);
		}
		public Meter(string key, IEnumerable<Metric> metrics)
		{
			this.Key = key;
			this.Metrics = new MetricCollection(metrics);
		}
		#endregion

		#region 公共字段
		public string Key;
		public MetricCollection Metrics;
		#endregion

		#region 公共属性
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty => this.Metrics == null || this.Metrics.Count == 0;
		#endregion

		#region 重写方法
		public bool Equals(Meter other) => string.Equals(this.Key, other.Key);
		public override bool Equals(object obj) => obj is Meter other && this.Equals(other);
		public override int GetHashCode() => string.IsNullOrEmpty(this.Key) ? 0 : this.Key.ToUpperInvariant().GetHashCode();
		public override string ToString() => $"{this.Key}({this.Metrics.Count})";
		#endregion

		#region 重写符号
		public static bool operator ==(Meter left, Meter right) => left.Equals(right);
		public static bool operator !=(Meter left, Meter right) => !(left == right);
		#endregion
	}

	public struct Metric(string key, string code, object value) : IEquatable<Metric>
	{
		#region 公共字段
		public string Key = key;
		public string Code = code;
		public object Value = value;
		#endregion

		#region 公共属性
		[System.Text.Json.Serialization.JsonIgnore]
		[Zongsoft.Serialization.SerializationMember(Ignored = true)]
		public bool HasValue => this.Value != null;
		#endregion

		#region 重写方法
		public bool Equals(Metric other) => string.Equals(this.Key, other.Key);
		public override bool Equals(object obj) => obj is Metric other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Key.ToUpperInvariant());
		public override string ToString() => $"{this.Key}={this.Value}";
		#endregion

		#region 重写符号
		public static bool operator ==(Metric left, Metric right) => left.Equals(right);
		public static bool operator !=(Metric left, Metric right) => !(left == right);
		#endregion
	}

	public class MetricCollection : KeyedCollection<string, Metric>
	{
		#region 构造函数
		public MetricCollection() { }
		public MetricCollection(IEnumerable<Metric> metrics) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(metrics != null)
			{
				foreach(var metric in metrics)
					this.Items.Add(metric);
			}
		}
		#endregion

		#region 公共方法
		public Metric Add(string key, string code, object value)
		{
			var metric = new Metric(key, code, value);
			this.Items.Add(metric);
			return metric;
		}

		public bool TryAdd(string key, string code, object value, out Metric result)
		{
			if(Contains(key))
			{
				result = default;
				return false;
			}

			result = new Metric(key, code, value);
			this.Items.Add(result);
			return true;
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Metric metric) => metric.Key;
		#endregion
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Samples;

public sealed class Module() : Zongsoft.Services.ApplicationModule<Module.EventRegistry>(string.Empty)
{
	public static readonly Module Current = new();

	public sealed class EventRegistry : EventRegistryBase
	{
		#region 构造函数
		public EventRegistry() : base(string.Empty)
		{
			this.Event(AcquirerEvent.Acquired);
			this.Acquirer = new AcquirerEvent(this);
		}
		#endregion

		#region 公共属性
		public AcquirerEvent Acquirer { get; }
		#endregion

		public sealed class AcquirerEvent
		{
			#region 静态字段
			public static readonly EventDescriptor Acquired = new($"{nameof(Acquirer)}.{nameof(Acquired)}", [typeof(Models.Meter)]);
			#endregion

			#region 成员字段
			private readonly EventRegistry _registry;
			#endregion

			#region 构造函数
			internal AcquirerEvent(EventRegistry registry) => _registry = registry;
			#endregion

			#region 公共方法
			public ValueTask<bool> OnAcquiredAsync(Models.Meter argument, CancellationToken cancellation = default) => this.OnAcquiredAsync(argument, null, cancellation);
			public ValueTask<bool> OnAcquiredAsync(Models.Meter argument, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation = default) => _registry.RaiseAsync(Acquired.Name, argument, parameters, cancellation);
			#endregion
		}
	}
}

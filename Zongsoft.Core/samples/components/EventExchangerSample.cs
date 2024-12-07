using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Collections;
using Zongsoft.Configuration;
using Zongsoft.Messaging.Mqtt;

namespace Zongsoft.Components.Samples;

public class EventExchangerSample
{
	private readonly string CONNECTION_STRING = $"server=121.41.239.123;username=program;password=Program@2025!MQTT;client=Zongsoft.Sample#{Random.Shared.Next():X}";

	private volatile int _count;
	private readonly Handler _handler;

	public EventExchangerSample()
	{
		//创建MQTT消息队列
		var queue = new MqttQueue("Mqtt", new ConnectionSettings("Mqtt", CONNECTION_STRING));

		//创建消息交换器并将其添加到应用上下文的工作者集合中
		ApplicationContext.Current.Workers.Add(this.Exchanger = new EventExchanger(queue, new EventExchangerOptions("Samples/Events")));

		//挂载事件处理程序
		Module.Current.Events.Register($"{nameof(Module.Current.Events.Acquirer)}.{nameof(Module.EventRegistry.AcquirerEvent.Acquired)}", _handler = new Handler(this.Exchanger));
	}

	public EventExchanger Exchanger { get; }

	public void Reset()
	{
		_count = 0;
		_handler.Reset();
	}

	public ValueTask<bool> RaiseAsync(int round, int quantity, CancellationToken cancellation = default)
	{
		var result = Parallel.For(0, round > 0 ? round : int.MaxValue, new ParallelOptions { CancellationToken = cancellation }, async (index, state) =>
		{
			var meter = new Models.Meter($"Meter#{Interlocked.Increment(ref _count)}", $"Code#{Random.Shared.NextInt64()}");

			for(int i = 0; i < Math.Max(quantity, 1); i++)
				meter.Metrics.Add($"Metric#{i + 1}", $"Code#{i + 1}", Random.Shared.NextDouble());

			await Module.Current.Events.Acquirer.OnAcquiredAsync(meter, Parameters.Parameter("Exchanger", this.Exchanger.Identifier));
		});

		return ValueTask.FromResult(result.IsCompleted);
	}

	private sealed class Handler(EventExchanger exchanger) : HandlerBase<Models.Meter>
	{
		private volatile int _count;
		private readonly EventExchanger _exchanger = exchanger;

		public void Reset() => _count = 0;

		protected override ValueTask OnHandleAsync(Models.Meter argument, Parameters parameters, CancellationToken cancellation)
		{
			var count = Interlocked.Increment(ref _count);

			if(parameters.TryGetValue("Exchanger", out var value) && value is uint identifier && identifier == _exchanger.Identifier)
			{
				Console.WriteLine($"Sent#{count} : {argument}");
				return ValueTask.CompletedTask;
			}

			Console.WriteLine($"{count} : {argument}");
			return ValueTask.CompletedTask;
		}
	}
}

using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Collections;
using Zongsoft.Configuration;
using Zongsoft.Messaging.ZeroMQ;

namespace Zongsoft.Components.Samples;

public class EventExchangerSample : IAsyncDisposable
{
	private readonly string CONNECTION_STRING = $"server=127.0.0.1;client=Zongsoft.Sample#{Random.Shared.Next():X}";

	private volatile int _count;
	private readonly Handler _handler;
	private readonly ZeroQueue _queue;
	private readonly ZeroQueueEventChannel _channel;

	public EventExchangerSample(bool remote = true)
	{
		if(remote)
		{
			_queue = new ZeroQueue("ZeroMQ", Zongsoft.Messaging.ZeroMQ.Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING));
			_channel = new ZeroQueueEventChannel(_queue);
			EventExchanger.Instance.Channels.Add(_channel);
		}

		//挂载事件处理程序
		Module.Current.Events.Register($"{nameof(Module.Current.Events.Acquirer)}.{nameof(Module.EventRegistry.AcquirerEvent.Acquired)}", _handler = new Handler());
	}

	/// <summary>重置当前范例的发送和接收计数。</summary>
	public void Reset()
	{
		_count = 0;
		_handler.Reset();
	}

	/// <summary>并发激发指定轮数的采集事件，并等待全部激发操作完成。</summary>
	/// <param name="round">事件轮数；小于等于零时使用 <see cref="int.MaxValue"/>。</param>
	/// <param name="quantity">每项事件的指标数量，至少为一项。</param>
	/// <param name="cancellation">取消事件激发的令牌。</param>
	/// <returns>全部轮次完成后返回 <see langword="true"/>。</returns>
	public async ValueTask<bool> RaiseAsync(int round, int quantity, CancellationToken cancellation = default)
	{
		var stopwatch = new Stopwatch();
		stopwatch.Start();

		await Parallel.ForAsync(0, round > 0 ? round : int.MaxValue, new ParallelOptions { CancellationToken = cancellation }, async (index, token) =>
		{
			var meter = new Models.Meter($"Meter#{Interlocked.Increment(ref _count)}", $"Code#{Random.Shared.NextInt64()}");

			for(int i = 0; i < Math.Max(quantity, 1); i++)
				meter.Metrics.Add($"Metric#{i + 1}", $"Code#{i + 1}", Random.Shared.NextDouble());

			await Module.Current.Events.Acquirer.OnAcquiredAsync(meter, token);
		});

		Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
		return true;
	}

	/// <summary>等待投递完成，然后释放范例拥有的事件订阅和消息队列。</summary>
	/// <returns>表示资源释放完成的任务。</returns>
	public async ValueTask DisposeAsync()
	{
		await EventExchanger.Instance.StopAsync();
		Module.Current.Events.Unregister(Module.EventRegistry.AcquirerEvent.Acquired.Name, _handler);

		if(_channel != null)
		{
			EventExchanger.Instance.Channels.Remove(_channel);
			await _channel.DisposeAsync();
		}

		if(_queue != null)
			await _queue.DisposeAsync();
	}

	private sealed class Handler : HandlerBase<Models.Meter>
	{
		private volatile int _count;

		public void Reset() => _count = 0;

		protected override ValueTask OnHandleAsync(Models.Meter argument, Parameters parameters, CancellationToken cancellation)
		{
			var count = Interlocked.Increment(ref _count);

			var text = new StringBuilder($"Received#{count} : {argument}");
			FillMeterInfo(text, argument.Metrics);
			Console.WriteLine(text.ToString());

			return ValueTask.CompletedTask;
		}

		private static void FillMeterInfo(StringBuilder text, Models.Meter.MetricCollection metrics)
		{
			if(metrics == null || metrics.Count == 0)
				return;

			for(int i = 0; i < metrics.Count; i++)
			{
				text.AppendLine();
				text.Append($"  [{i + 1}]{metrics[i]}");
			}
		}
	}
}

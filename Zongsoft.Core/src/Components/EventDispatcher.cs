/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Components;

/// <summary>在调用环境允许时执行事件；拥有有界的延期工作及其生命周期。</summary>
/// <remarks>启动后才能调度。停止时放弃尚未就绪的工作，等待已就绪的投递结束。延期数据只保存在内存中。</remarks>
public class EventDispatcher : WorkerBase, IEventDispatcher
{
	#region 成员字段
	private Session _session;
	private IFeaturePipeline<EventContext> _pipeline;
	private readonly IFeatureProvider _provider;
	#endregion

	#region 构造函数
	/// <summary>使用 <see cref="FeatureProvider.Default"/> 创建调度器。</summary>
	public EventDispatcher() : this(null) { }

	/// <summary>创建具有指定特性提供者和容量的调度器。</summary>
	/// <param name="provider">捕获运行时特性的提供者；<see langword="null"/> 使用 <see cref="FeatureProvider.Default"/>。显式指定的提供者负责全部特性来源。</param>
	/// <param name="capacity">未完成工作数上限，必须大于零。</param>
	public EventDispatcher(IFeatureProvider provider, int capacity = 1024)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

		_provider = provider ?? FeatureProvider.Default;
		this.Capacity = capacity;
	}
	#endregion

	#region 公共属性
	/// <summary>获取尚未结束的工作数量上限；达到上限时拒绝接收，不等待环境释放容量。</summary>
	public int Capacity { get; }

	/// <summary>获取或设置环境允许后用于实际投递的策略管线；仅允许在停止时设置。</summary>
	/// <value>可选的投递策略管线；<see langword="null"/> 表示直接执行投递。</value>
	/// <remarks>管线实例在多次投递及启停之间复用，必须支持并发执行；其资源由装配方管理。等待环境的时间不计入管线，运行时环境特性不会用于构建策略。</remarks>
	/// <exception cref="InvalidOperationException">调度器未停止。</exception>
	public IFeaturePipeline<EventContext> Pipeline
	{
		get => _pipeline;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException();

			_pipeline = value;
		}
	}
	#endregion

	#region 公共方法
	/// <summary>捕获调用环境的特性，并立即执行或接收延期事件。</summary>
	/// <param name="argument">待调度的事件上下文。</param>
	/// <param name="executor">环境允许后执行远程投递的执行器。</param>
	/// <param name="cancellation">取消本次等待及投递的令牌。</param>
	/// <returns>立即执行完成或延期接收完成的任务。</returns>
	public ValueTask DispatchAsync(EventContext argument, IExecutor<EventContext> executor, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(argument);
		ArgumentNullException.ThrowIfNull(executor);
		cancellation.ThrowIfCancellationRequested();

		var session = Volatile.Read(ref _session) ?? throw new InvalidOperationException(Properties.Resources.EventDispatcher_Closed_Message);

		//必须在原调用流捕获环境，后台工作只使用绑定后的特性。
		var features = (_provider.GetFeatures() ?? [])
			.Distinct<IFeature>(ReferenceEqualityComparer.Instance)
			.ToArray();

		return session.DispatchAsync(argument, executor, features, cancellation);
	}
	#endregion

	#region 扩展方法
	/// <summary>通过 <see cref="IPredication"/> 求值执行条件；派生类可扩展环境能力，未知特性不能静默忽略。</summary>
	/// <param name="feature">已捕获的运行时特性。</param>
	/// <param name="argument">待调度的事件上下文，其参数集同时传给条件求值方法。</param>
	/// <param name="cancellation">条件求值的取消令牌；调度器使用 <see cref="CancellationToken.None"/>，单次事件的取消作用于结果等待。</param>
	/// <returns>允许执行返回 <see langword="true"/>，放弃执行返回 <see langword="false"/>。</returns>
	protected virtual ValueTask<bool> PredicateAsync(IFeature feature, EventContext argument, CancellationToken cancellation) => feature switch
	{
		IPredication predicate => predicate.PredicateAsync(argument, argument.Parameters, cancellation),
		_ => throw new NotSupportedException(string.Format(Properties.Resources.EventDispatcher_FeatureUnsupported_Message, feature?.GetType())),
	};

	/// <summary>创建用于接收事件的调度会话。</summary>
	/// <param name="args">启动参数。</param>
	/// <param name="cancellation">启动操作的取消令牌。</param>
	/// <returns>调度会话初始化完成的任务。</returns>
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		Volatile.Write(ref _session, new Session(this, _pipeline));
		return Task.CompletedTask;
	}

	/// <summary>关闭接收入口，放弃尚未就绪的事件并等待允许投递的事件结束。</summary>
	/// <param name="args">停止参数。</param>
	/// <param name="cancellation">停止操作的取消令牌；会话清理开始后等待其完整结束。</param>
	/// <returns>当前调度会话结束的任务。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var session = Volatile.Read(ref _session);

		if(session != null)
		{
			await session.StopAsync().ConfigureAwait(false);
			Interlocked.CompareExchange(ref _session, null, session);
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class Session(EventDispatcher owner, IFeaturePipeline<EventContext> pipeline)
	{
		private bool _closed;
		private readonly HashSet<Work> _works = [];
		private readonly Dictionary<IFeature, List<Work>> _queues = new(ReferenceEqualityComparer.Instance);

		public async ValueTask DispatchAsync(EventContext argument, IExecutor<EventContext> executor, IFeature[] features, CancellationToken cancellation)
		{
			Work work;

			lock(_works)
			{
				if(_closed)
					throw new InvalidOperationException(Properties.Resources.EventDispatcher_Closed_Message);
				if(_works.Count >= owner.Capacity)
					throw new InvalidOperationException(Properties.Resources.EventDispatcher_CapacityExceeded_Message);

				var previous = features
					.Where(_queues.ContainsKey)
					.Select(feature => _queues[feature][^1].Finished.Task)
					.Distinct()
					.ToArray();

				work = new Work(this, argument, executor, pipeline, features, previous, cancellation);
				_works.Add(work);

				foreach(var feature in features)
				{
					if(!_queues.TryGetValue(feature, out var queue))
						_queues.Add(feature, queue = []);

					queue.Add(work);
				}
			}

			try
			{
				//提供者回调和负载序列化不持有会话锁。
				await work.PrepareAsync(owner).ConfigureAwait(false);
			}
			catch
			{
				this.Release(work);
				throw;
			}

			if(!work.Deferred)
			{
				await work.ExecuteAsync().ConfigureAwait(false);
				return;
			}

			//不把原调用流的环境事务、请求服务等带入后台投递。
			if(ExecutionContext.IsFlowSuppressed())
				_ = Task.Run(work.ExecuteAsync);
			else
			{
				using(ExecutionContext.SuppressFlow())
					_ = Task.Run(work.ExecuteAsync);
			}
		}

		public async Task StopAsync()
		{
			Work[] works;

			lock(_works)
			{
				_closed = true;
				works = [.. _works];
			}

			foreach(var work in works)
				work.CancelWaiting();

			await Task.WhenAll(works.Select(work => work.Finished.Task)).ConfigureAwait(false);
		}

		public void Release(Work work)
		{
			work.Dispose();

			lock(_works)
			{
				foreach(var feature in work.Features)
				{
					var queue = _queues[feature];
					queue.Remove(work);

					if(queue.Count == 0)
						_queues.Remove(feature);
				}

				_works.Remove(work);
				work.Finished.TrySetResult();
			}
		}
	}

	private sealed class Work(Session session, EventContext argument, IExecutor<EventContext> executor, IFeaturePipeline<EventContext> pipeline, IFeature[] features, Task[] previous, CancellationToken cancellation) : IDisposable
	{
		private byte[] _data;
		private Task<bool> _decision;
		private bool _prepared, _stopping, _disposed;
		private readonly object _sync = new();
		private readonly List<Task<bool>> _conditions = [];
		private readonly CancellationTokenSource _waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

		public IFeature[] Features => features;
		public bool Deferred { get; private set; }
		public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public async ValueTask PrepareAsync(EventDispatcher owner)
		{
			_decision = this.WaitAsync(owner);
			_ = _decision.ContinueWith(failed =>
				_ = failed.Exception,
				CancellationToken.None,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			lock(_sync)
			{
				_prepared = true;

				if(_stopping)
					this.CancelWaiting();
			}

			//只观察已完成的决定；未完成时接收快照，不能在调用流等待环境。
			this.Deferred = !_decision.IsCompleted || (await _decision.ConfigureAwait(false) && previous.Any(task => !task.IsCompleted));
			if(this.Deferred)
				_data = Events.Marshaler.Marshal(argument);
		}

		public void CancelWaiting()
		{
			lock(_sync)
			{
				_stopping = true;

				if(!_disposed && _prepared && _conditions.Any(task => !task.IsCompletedSuccessfully || !task.Result))
					_waiting.Cancel();
			}
		}

		public async Task ExecuteAsync()
		{
			try
			{
				if(!await _decision.ConfigureAwait(false))
					return;

				await Task.WhenAll(previous)
					.WaitAsync(cancellation)
					.ConfigureAwait(false);

				cancellation.ThrowIfCancellationRequested();
				var context = argument;

				if(this.Deferred)
				{
					var (argumentType, value, eventParameters) = Events.Marshaler.Unmarshal(argument.Registry[argument.Name], _data);
					context = argument.Registry.GetContext(argumentType, argument.Name, value, eventParameters);
				}

				var parameters = new Collections.Parameters();

				if(pipeline == null)
					await InvokeAsync(context, parameters, cancellation).ConfigureAwait(false);
				else
					await pipeline.ExecuteAsync(InvokeAsync, context, parameters, cancellation).ConfigureAwait(false);
			}
			catch(OperationCanceledException) when(this.Deferred && _waiting.IsCancellationRequested) { }
			catch(Exception exception) when(this.Deferred)
			{
				try
				{
					Diagnostics.Logging.GetLogging(typeof(EventDispatcher)).Error(exception);
				}
				catch { /*日志后端失败不能产生未观察的后台异常。*/ }
			}
			finally
			{
				session.Release(this);
			}

			ValueTask InvokeAsync(EventContext context, Collections.Parameters parameters, CancellationToken token) =>
				((IExecutor)executor).ExecuteAsync(new ExecutorContext<EventContext>(executor, context, parameters, features), token);
		}

		private async Task<bool> WaitAsync(EventDispatcher owner)
		{
			var tasks = new List<Task<bool>>(features.Length);
			var token = _waiting.Token;

			foreach(var feature in features)
			{
				//条件可以返回共享完成任务；取消仅作用于本次调度对结果的等待。
				var task = owner.PredicateAsync(feature, argument, CancellationToken.None).AsTask();

				//任一条件否决后，仍观察其它等待可能发生的故障。
				_ = task.ContinueWith(failed =>
					_ = failed.Exception,
					CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

				tasks.Add(task);
				_conditions.Add(task);
			}

			while(tasks.Count > 0)
			{
				var completed = await Task.WhenAny(tasks).WaitAsync(token).ConfigureAwait(false);

				if(!await completed.ConfigureAwait(false))
					return false;

				tasks.Remove(completed);
			}

			return true;
		}

		public void Dispose()
		{
			lock(_sync)
			{
				_waiting.Cancel();
				_waiting.Dispose();
				_disposed = true;
			}
		}
	}
	#endregion
}

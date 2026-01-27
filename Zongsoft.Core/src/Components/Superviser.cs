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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Caching;

namespace Zongsoft.Components;

public partial class Superviser<T> : ISuperviser<T>, IDisposable
{
	#region 事件声明
	public event EventHandler<SupervisedEventArgs> Supervised;
	public event EventHandler<UnsupervisedEventArgs> Unsupervised;
	#endregion

	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 私有变量
	private volatile int _disposing;
	#endregion

	#region 成员字段
	private MemoryCache _cache;
	private MemoryCacheScanner _scanner;
	private SupervisableOptions _options;
	#endregion

	#region 构造函数
	public Superviser(SupervisableOptions options = null) : this(TimeSpan.Zero, options) { }
	public Superviser(TimeSpan frequency, SupervisableOptions options = null)
	{
		//确保选项不为空
		_options = options ?? new();

		//确保内存缓存的扫描频率不能过高，因为扫描频率过高可能会导致监测精度不够
		frequency = TimeSpanUtility.Clamp(frequency, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));

		_cache = new MemoryCache(frequency);
		_scanner = new MemoryCacheScanner(_cache);
		_cache.Evicted += this.OnEvicted;
		_scanner.Start();
	}
	#endregion

	#region 公共属性
	public int Count => _cache?.Count ?? 0;
	public IObservable<T> this[object key] => key != null && _cache.TryGetValue(key, out var value) && value is Observer observer ? observer.Observable : null;
	public bool IsDisposed => _disposing == DISPOSED;
	public bool IsDisposing => _disposing == DISPOSING;

	/// <summary>获取或设置默认的监测选项设置。</summary>
	public SupervisableOptions Options
	{
		get => _options;
		set => _options = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public void Clear() => _cache?.Clear();
	public bool Contains(object key) => key != null && _cache.Contains(key);

	public IDisposable Supervise(IObservable<T> observable) => this.Supervise(null, observable);
	public IDisposable Supervise(object key, IObservable<T> observable)
	{
		if(observable == null)
			throw new ArgumentNullException(nameof(observable));

		//获取或创建一个监视被观察对象的观察者
		var observer = _cache.GetOrCreate(key ?? observable, key =>
		{
			//获取当前被观察对象的生命周期，如果其未设置则应用监测器的默认值
			var lifecycle = this.GetOptions(observable).Lifecycle;

			//创建观察器对象
			var observer = new Observer();

			//注意：如果缓存项的有效期为零则不会进行缓存驱逐事件的监听，因此对于无限期的被观察者必须返回对应的缓存期限设置为最大值
			return (
				observer,
				Notification.GetToken(observer.Failure),
				lifecycle > TimeSpan.Zero ? lifecycle : TimeSpan.MaxValue);
		});

		//如果初始化成功则表示是第一次监视该被观察对象
		if(observer.Initialize(this, observable, key))
		{
			//订阅被观察对象的行为
			observer.Subscribe();

			//触发“Supervised”事件
			this.OnSupervised(key, observable);
		}

		//返回观察者订阅凭证
		return observer.Subscribe();
	}

	public bool Unsupervise(object key) => this.Unsupervise(key, out _);
	public bool Unsupervise(object key, out IObservable<T> observable)
	{
		if(key != null && _cache.Remove(key, out var value) && value is Observer observer)
		{
			observable = observer.Observable;
			return true;
		}

		observable = null;
		return false;
	}
	#endregion

	#region 驱逐事件
	private void OnEvicted(object sender, CacheEvictedEventArgs args)
	{
		if(args.Value is Observer observer)
		{
			var reason = args.Reason switch
			{
				CacheEvictedReason.Expired => SupervisableReason.Inactived,
				CacheEvictedReason.Depended => SupervisableReason.Failed,
				_ => SupervisableReason.Manual,
			};

			switch(observer.Observable)
			{
				case ISupervisable<T> supervisable:
					supervisable.OnUnsupervised(this, reason);
					break;
				default:
					observer.Observable?.Subscribe(null);
					break;
			}

			//触发“Unsupervised”事件
			this.OnUnsupervised(observer.CachedKey, observer.Observable, reason);

			//释放观察者资源
			observer.Dispose();
		}

		//如果当前监测器已被处置
		if(_disposing != 0)
		{
			var cache = _cache;

			if(cache != null && cache.Count == 0)
			{
				cache.Evicted -= this.OnEvicted;
				cache.Dispose();
				_cache = null;
			}
		}
	}
	#endregion

	#region 错误回调
	protected virtual bool OnError(object key, IObservable<T> observable, Exception exception, uint count) => this.GetOptions(observable).HasErrorLimit(out var limit) && count > limit;
	#endregion

	#region 事件触发
	protected virtual void OnSupervised(object key, IObservable<T> observable) => this.Supervised?.Invoke(this, new(key, observable));
	protected virtual void OnUnsupervised(object key, IObservable<T> observable, SupervisableReason reason) => this.Unsupervised?.Invoke(this, new(key, observable, reason));
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.GetType().Name}] {_options}";
	#endregion

	#region 处置方法
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_disposing = DISPOSED;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			_scanner?.Stop();
			_cache?.Clear();
		}

		this.Supervised = null;
		this.Unsupervised = null;

		_scanner?.Dispose();
		_scanner = null;
	}
	#endregion

	#region 私有方法
	private SupervisableOptions GetOptions(object observable) => observable is ISupervisable<T> supervisable && supervisable.Options != null ? supervisable.Options : _options;
	private void Touch(object key)
	{
		if(key != null && _disposing == 0)
			_cache?.Contains(key);
	}
	#endregion

	#region 嵌套子类
	private sealed class Observer : IObserver<T>, IDisposable, IEquatable<Observer>
	{
		#region 私有变量
		private uint _errors;
		private object _cachedKey;
		private Superviser<T> _superviser;
		private IObservable<T> _observable;
		private IDisposable _subscriber;
		private readonly CancellationTokenSource _failure = new();

		#if NET9_0_OR_GREATER
		private readonly Lock _lock = new();
		#else
		private readonly object _lock = new();
		#endif
		#endregion

		#region 初始方法
		private volatile int _initialized;
		internal bool Initialize(Superviser<T> superviser, IObservable<T> observable, object cachedKey)
		{
			if(Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
			{
				_cachedKey = cachedKey;
				_superviser = superviser;
				_observable = observable;
				return true;
			}

			return false;
		}
		#endregion

		#region 观察方法
		public void OnCompleted()
		{
			//被观察对象已终止，则将其从监测器中移除
			_superviser?.Unsupervise(_cachedKey ?? _observable);
		}

		public void OnNext(T value)
		{
			//重置最近的错误计数值
			_errors = 0;

			//更新被观察对象的最后访问时间以顺延过期
			_superviser?.Touch(_cachedKey ?? _observable);
		}

		public void OnError(Exception exception)
		{
			var superviser = _superviser;
			var observable = _observable;

			if(superviser == null || observable == null)
				return;

			//通知监测器进行错误处理，如果返回真则取消观察，否则更新被观察对象的最后访问时间以顺延过期
			if(superviser.OnError(_cachedKey, observable, exception, Interlocked.Increment(ref _errors)))
				_failure.Cancel();
			else
				superviser.Touch(_cachedKey ?? _observable);
		}
		#endregion

		#region 公共属性
		internal object CachedKey => _cachedKey;
		internal IObservable<T> Observable => _observable;
		internal CancellationTokenSource Failure => _failure;
		#endregion

		#region 订阅方法
		/// <summary>订阅被观察对象，即将观察者注入给被观察对象以挂载被观察对象的通知。</summary>
		/// <remarks>注意：该方法实现确保只能进行一次订阅，以避免重复订阅可能导致的重复通知。</remarks>
		/// <returns>返回的订阅凭证。</returns>
		public IDisposable Subscribe()
		{
			var superviser = _superviser;
			var observable = _observable;

			if(superviser == null || observable == null)
				return null;

			if(_subscriber != null)
				return _subscriber;

			lock(_lock)
			{
				if(_subscriber != null)
					return _subscriber;

				return _subscriber = observable.Subscribe(this);
			}
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			_cachedKey = null;
			_subscriber = null;
			_superviser = null;
			_observable = null;
		}
		#endregion

		#region 重写方法
		public bool Equals(Observer other) => other is not null && _superviser == other._superviser && _observable == other._observable;
		public override bool Equals(object obj) => this.Equals(obj as Observer);
		public override int GetHashCode() => HashCode.Combine(_superviser, _observable);
		public override string ToString() => _observable?.ToString() ?? base.ToString();
		#endregion
	}
	#endregion
}

#if NET9_0_OR_GREATER
partial class Superviser<T> : IEnumerable<IObservable<T>>
{
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IObservable<T>> GetEnumerator()
	{
		var cache = _cache ?? throw new ObjectDisposedException(this.GetType().FullName);

		foreach(var key in cache.Keys)
		{
			if(key is IObservable<T> observable)
				yield return observable;
			else if(cache.TryGetValue(key, out var value) && value is Observer observer)
				yield return observer.Observable;
		}
	}
}
#endif

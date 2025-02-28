﻿/*
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
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Caching;

namespace Zongsoft.Components;

public partial class Superviser<T> : ISuperviser<T>, IDisposable
{
	#region 成员字段
	private MemoryCache _cache;
	private ConcurrentDictionary<object, IObservable<T>> _keys;
	private MemoryCacheScanner _scanner;
	private SupervisableOptions _options;
	#endregion

	#region 构造函数
	public Superviser(SupervisableOptions options = null)
	{
		_options = options ?? new();
		_keys = new(KeyEqualityComparer.Instance);

		//确保内存缓存的扫描频率不能过高，因为扫描频率过高可能会导致监测精度不够
		var frequency = _options.Lifecycle > TimeSpan.Zero ? _options.Lifecycle : TimeSpan.FromSeconds(60);
		if(frequency > TimeSpan.FromSeconds(60))
			frequency = TimeSpan.FromSeconds(60);

		_cache = new MemoryCache(frequency);
		_scanner = new MemoryCacheScanner(_cache);
		_cache.Evicted += this.OnEvicted;
		_scanner.Start();
	}
	#endregion

	#region 公共属性
	public int Count => _cache?.Count ?? 0;
	public ICollection<object> Keys => _keys.Keys;
	public IObservable<T> this[object key] => key != null && _keys.TryGetValue(key, out var observable) ? observable : null;

	/// <summary>获取或设置默认的监测选项设置。</summary>
	public SupervisableOptions Options
	{
		get => _options;
		set => _options = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public void Clear() => _cache?.Clear();
	public bool Contains(object key) => key is IObservable<T> ? _cache.Contains(key) : _keys.ContainsKey(key);
	public bool Contains(IObservable<T> observable) => _cache.Contains(observable);

	public IDisposable Supervise(IObservable<T> observable) => this.Supervise(null, observable);
	public IDisposable Supervise(object key, IObservable<T> observable)
	{
		if(observable == null)
			throw new ArgumentNullException(nameof(observable));

		//获取或创建一个监视被观察对象的观察者
		var observer = _cache.GetOrCreate(observable, observable =>
		{
			//获取当前被观察对象的生命周期，如果其未设置则应用监测器的默认值
			var lifecycle = this.GetOptions(observable).Lifecycle;

			//注意：如果缓存项的有效期为零则不会进行缓存驱逐事件的监听，因此对于无限期的被观察者必须返回对应的缓存期限设置为最大值
			return (new Observer(this, (IObservable<T>)observable), lifecycle > TimeSpan.Zero ? lifecycle : TimeSpan.MaxValue, key);
		});

		if(key != null)
			_keys.TryAdd(key, observable);

		//订阅被观察对象的行为
		observer.Subscribe();

		//返回观察者订阅凭证
		return observer;
	}

	public bool Unsupervise(IObservable<T> observable)
	{
		//获取被观察对象并执行取消对被观察对象的监测(取消订阅)
		if(observable != null && _cache.Remove(observable, out var value))
		{
			if(value is Observer observer)
				observer.Unsubscribe();
			else if(value is IDisposable disposable)
				disposable.Dispose();

			return true;
		}

		return false;
	}

	public bool Unsupervise(object key, out IObservable<T> observable)
	{
		//获取被观察对象并执行取消对被观察对象的监测(取消订阅)
		if(_keys.TryRemove(key, out observable))
			return this.Unsupervise(observable);

		return false;
	}
	#endregion

	#region 驱逐事件
	private void OnEvicted(object sender, CacheEvictedEventArgs args)
	{
		if(args.State != null)
			_keys.TryRemove(args.State, out _);

		this.OnUnsupervised((IObservable<T>)args.Key);
	}

	protected virtual void OnUnsupervised(IObservable<T> observable)
	{
		switch(observable)
		{
			case ISupervisable<T> supervisable:
				supervisable.OnUnsupervised(this);
				break;
			case IDisposable disposable:
				disposable.Dispose();
				break;
		}
	}
	#endregion

	#region 错误回调
	protected virtual bool OnError(IObservable<T> observable, Exception exception, uint count) => this.GetOptions(observable).HasErrorLimit(out var limit) && count > limit;
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.GetType().Name}] {_options}";
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			_cache?.Clear();
			_scanner?.Stop();
		}

		var cache = _cache;
		if(cache != null)
			cache.Evicted -= this.OnEvicted;

		_cache = null;
		_scanner = null;
	}
	#endregion

	#region 私有方法
	private void Touch(IObservable<T> observable) => _cache.Contains(observable);
	private SupervisableOptions GetOptions(object observable) => observable is ISupervisable<T> supervisable && supervisable.Options != null ? supervisable.Options : _options;
	#endregion

	#region 嵌套子类
	private sealed class KeyEqualityComparer : EqualityComparer<object>
	{
		public static readonly KeyEqualityComparer Instance = new();
		public override int GetHashCode(object obj) => obj is string text ? text.ToUpperInvariant().GetHashCode() : Default.GetHashCode(obj);
		public override bool Equals(object x, object y)
		{
			if(x == null)
				return y == null;

			if(y == null)
				return false;

			if(x is string a && y is string b)
				return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

			return Default.Equals(x, y);
		}
	}

	private sealed class Observer : IObserver<T>, IDisposable, IEquatable<IObservable<T>>
	{
		#region 私有变量
		private readonly object _lock;
		private Superviser<T> _superviser;
		private IObservable<T> _observable;
		private IDisposable _subscriber;
		private uint _errors;
		#endregion

		#region 构造函数
		public Observer(Superviser<T> superviser, IObservable<T> observable)
		{
			_lock = new();
			_superviser = superviser;
			_observable = observable;
		}
		#endregion

		#region 观察方法
		public void OnCompleted()
		{
			//被观察对象已终止，则将其从监测器中移除
			_superviser.Unsupervise(_observable);
		}

		public void OnNext(T value)
		{
			//重置最近的错误计数值
			_errors = 0;

			//更新被观察对象的最后访问时间以顺延过期
			_superviser.Touch(_observable);
		}

		public void OnError(Exception exception)
		{
			//通知监测器进行错误处理，如果返回真则取消观察，否则更新被观察对象的最后访问时间以顺延过期
			if(_superviser.OnError(_observable, exception, Interlocked.Increment(ref _errors)))
				_superviser.Unsupervise(_observable);
			else
				_superviser.Touch(_observable);
		}
		#endregion

		#region 公共属性
		public bool IsSubscribed => _subscriber != null;
		#endregion

		#region 订阅方法
		/// <summary>订阅被观察对象，即将观察者注入给被观察对象以挂载被观察对象的通知。</summary>
		/// <remarks>注意：该方法实现确保只能进行一次订阅，以避免重复订阅可能导致的重复通知。</remarks>
		/// <returns>返回的订阅凭证。</returns>
		public IDisposable Subscribe()
		{
			if(_superviser == null)
				return null;

			if(_subscriber != null)
				return _subscriber;

			lock(_lock)
			{
				if(_subscriber != null)
					return _subscriber;

				return _subscriber = _observable?.Subscribe(this);
			}
		}

		internal void Unsubscribe()
		{
			var superviser = Interlocked.Exchange(ref _superviser, null);

			if(superviser == null)
				return;

			lock(_lock)
			{
				//取消订阅
				_subscriber?.Dispose();

				//清空相关引用
				_subscriber = null;
				_observable = null;
			}
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var superviser = Interlocked.Exchange(ref _superviser, null);

			//将被观察对象从监测器中移除
			superviser?.Unsupervise(_observable);
		}
		#endregion

		#region 重写方法
		public bool Equals(IObservable<T> other) => other is not null && _observable == other;
		public override bool Equals(object obj) => obj is IObservable<T> observable && this.Equals(observable);
		public override int GetHashCode() => HashCode.Combine(_observable);
		#endregion
	}
	#endregion
}

#if NET9_0_OR_GREATER
partial class Superviser<T> : System.Collections.Generic.IEnumerable<IObservable<T>>
{
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
	public System.Collections.Generic.IEnumerator<IObservable<T>> GetEnumerator()
	{
		var cache = _cache ?? throw new ObjectDisposedException(this.GetType().FullName);

		foreach(var key in cache.Keys)
		{
			if(key is IObservable<T> observable)
				yield return observable;
		}
	}
}
#endif

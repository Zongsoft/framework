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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Caching;

namespace Zongsoft.Components;

public class Superviser<T> : IDisposable
{
	#region 成员字段
	private MemoryCache _cache;
	private MemoryCacheScanner _scanner;
	private SuperviserOptions _options;
	#endregion

	#region 构造函数
	public Superviser(SuperviserOptions options = null)
	{
		_options = options ?? new();
		_cache = new MemoryCache();
		_scanner = new MemoryCacheScanner(_cache);

		_cache.Evicted += this.OnEvicted;
		_scanner.Start();
	}
	#endregion

	#region 公共属性
	public int Count => _cache?.Count ?? 0;
	public SuperviserOptions Options
	{
		get => _options;
		set => _options = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public IDisposable Supervise(IObservable<T> observable)
	{
		if(observable == null)
			throw new ArgumentNullException(nameof(observable));

		return _cache.GetOrCreate(observable, key =>
		{
			var observer = new Observer(this, (IObservable<T>)key);
			return (observable.Subscribe(observer), _options.Lifecycle);
		});
	}

	public void Unsupervise(IObservable<T> observable)
	{
		if(observable == null)
			throw new ArgumentNullException(nameof(observable));

		if(_cache.Remove(observable, out var value) && value is IDisposable disposable)
			disposable.Dispose();
	}
	#endregion

	#region 驱逐事件
	private void OnEvicted(object sender, CacheEvictedEventArgs args) => this.OnEvicted((IObservable<T>)args.Key);
	protected virtual void OnEvicted(IObservable<T> observable) => (observable as IDisposable)?.Dispose();
	#endregion

	#region 错误回调
	protected virtual bool OnError(IObservable<T> observable, Exception exception, int count) => _options.HasErrorLimit(out var limit) && count > limit;
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
	#endregion

	#region 嵌套子类
	private sealed class Observer(Superviser<T> superviser, IObservable<T> observable) : IObserver<T>, IEquatable<IObservable<T>>
	{
		#region 私有变量
		private readonly Superviser<T> _superviser = superviser;
		private readonly IObservable<T> _observable = observable;
		private volatile int _errors;
		#endregion

		#region 观察方法
		public void OnCompleted() => _superviser.Unsupervise(_observable);
		public void OnNext(T value)
		{
			_errors = 0;
			_superviser.Touch(_observable);
		}

		public void OnError(Exception exception)
		{
			if(_superviser.OnError(_observable, exception, Interlocked.Increment(ref _errors)))
				_superviser.Unsupervise(_observable);
			else
				_superviser.Touch(_observable);
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

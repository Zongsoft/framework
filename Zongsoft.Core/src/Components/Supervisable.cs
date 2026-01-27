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

namespace Zongsoft.Components;

public abstract class Supervisable<T>(SupervisableOptions options = null) : ISupervisable<T>, IObservable<T>
{
	#region 成员字段
	private Subscriber _subscriber;
	private SupervisableOptions _options = options;
	#endregion

	#region 公共属性
	public SupervisableOptions Options
	{
		get => _options;
		set => _options = value;
	}
	#endregion

	#region 保护属性
	protected IObserver<T> Observer => _subscriber?.Observer;
	#endregion

	#region 订阅方法
	IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
	{
		//如果当前订阅者就是指定的被观察对象则返回当前订阅者
		if(_subscriber?.Observer == observer)
			return _subscriber;

		//释放原有订阅者(取消观察)
		_subscriber?.Dispose();

		//创建订阅者(即观察者令牌)
		_subscriber = new(observer, this);

		//完成订阅通知
		this.OnSubscribed(observer);

		//返回订阅者
		return _subscriber;
	}
	#endregion

	#region 订阅通知
	protected virtual void OnSubscribed(IObserver<T> observer) { }
	#endregion

	#region 终止监视
	void ISupervisable<T>.OnUnsupervised(ISuperviser<T> superviser, SupervisableReason reason)
	{
		_subscriber = null;
		this.OnUnsupervised(superviser, reason);
	}

	protected virtual void OnUnsupervised(ISuperviser<T> superviser, SupervisableReason reason) { }
	#endregion

	#region 嵌套子类
	private sealed class Subscriber(IObserver<T> observer, ISupervisable<T> supervisable) : IDisposable, IEquatable<Subscriber>
	{
		#region 常量定义
		private const int DISPOSED = 1;
		#endregion

		#region 私有变量
		private IObserver<T> _observer = observer;
		private ISupervisable<T> _supervisable = supervisable;
		#endregion

		#region 公共属性
		public IObserver<T> Observer => _observer;
		public bool IsDisposed => _disposed == DISPOSED;
		#endregion

		#region 处置方法
		private volatile int _disposed;
		public void Dispose()
		{
			var disposed = Interlocked.Exchange(ref _disposed, DISPOSED);

			if(disposed == 0)
			{
				_observer?.OnCompleted();
				_supervisable?.Subscribe(null);

				_observer = null;
				_supervisable = null;
			}
		}
		#endregion

		#region 重写方法
		public override int GetHashCode() => HashCode.Combine(_observer, _supervisable);
		public bool Equals(Subscriber other) => other is not null && this._observer == other._observer && this._supervisable == other._supervisable;
		public override bool Equals(object obj) => this.Equals(obj as Subscriber);
		#endregion

		#region 重写符号
		public static bool operator !=(Subscriber left, Subscriber right) => !(left == right);
		public static bool operator ==(Subscriber left, Subscriber right)
		{
			return left is null ? right is null : left.Equals(right);
		}
		#endregion
	}
	#endregion
}

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

using Microsoft.Extensions.Primitives;

namespace Zongsoft.Components;

public abstract class Supervisable<T> : IObservable<T>
{
	#region 成员字段
	private Subscriber _subscriber;
	#endregion

	#region 保护属性
	protected IObserver<T> Observer => _subscriber?.Observer;
	#endregion

	#region 订阅方法
	IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
	{
		if(observer == null)
			return null;

		//如果当前订阅者就是指定的被观察对象则返回当前订阅者
		if(_subscriber == observer)
			return _subscriber;

		//释放原有订阅者(取消观察)
		_subscriber?.Dispose();

		//创建一个订阅者(观察者的令牌)
		var subscriber = new Subscriber(observer);

		//注册订阅者的注销事件
		subscriber.Disposed.RegisterChangeCallback(
			subscriber =>
			{
				if(object.ReferenceEquals(_subscriber, subscriber))
					_subscriber = null;
			}, subscriber);

		//返回订阅者
		return _subscriber = subscriber;
	}
	#endregion

	#region 嵌套子类
	private sealed class Subscriber(IObserver<T> observer) : IEquatable<Subscriber>, IEquatable<IObserver<T>>, IDisposable
	{
		#region 私有变量
		private IObserver<T> _observer = observer;
		private CancellationTokenSource _cancellation = new();
		#endregion

		#region 公共属性
		public IObserver<T> Observer => _observer;
		public IChangeToken Disposed => Common.Notification.GetToken(_cancellation);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var cancellation = Interlocked.Exchange(ref _cancellation, null);

			if(cancellation != null)
			{
				cancellation.Cancel();
				cancellation.Dispose();

				_observer?.OnCompleted();
				_observer = null;
			}
		}
		#endregion

		#region 重写方法
		public override int GetHashCode() => HashCode.Combine(_observer);
		public bool Equals(Subscriber other) => other is not null && this._observer == other._observer;
		public bool Equals(IObserver<T> other) => this._observer == other;
		public override bool Equals(object obj) => obj switch
		{
			Subscriber subscriber => this.Equals(subscriber),
			IObserver<T> observer => this.Equals(observer),
			_ => false,
		};
		#endregion

		#region 重写符号
		public static bool operator ==(Subscriber left, Subscriber right) => left?._observer == right?._observer;
		public static bool operator !=(Subscriber left, Subscriber right) => left?._observer != right?._observer;
		public static bool operator ==(Subscriber left, IObserver<T> right) => left?._observer == right;
		public static bool operator !=(Subscriber left, IObserver<T> right) => left?._observer != right;
		public static bool operator ==(IObserver<T> left, Subscriber right) => left == right._observer;
		public static bool operator !=(IObserver<T> left, Subscriber right) => left != right._observer;
		#endregion
	}
	#endregion
}

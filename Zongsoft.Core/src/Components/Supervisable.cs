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

namespace Zongsoft.Components;

public abstract class Supervisable<T> : ISupervisable<T>, IObservable<T>
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

		//返回创建的订阅者(即观察者令牌)
		return _subscriber = this.OnSubscribe(observer);
	}

	private void Unsubscribe()
	{
		//释放原有订阅者(取消观察)
		_subscriber?.Dispose();
		_subscriber = null;
	}
	#endregion

	#region 订阅通知
	protected virtual Subscriber OnSubscribe(IObserver<T> observer) => new(this, observer);
	#endregion

	#region 终止监视
	void ISupervisable<T>.OnUnsupervised(ISuperviser<T> superviser) => this.OnUnsupervised(superviser);
	protected virtual void OnUnsupervised(ISuperviser<T> superviser) { }
	#endregion

	#region 嵌套子类
	protected sealed class Subscriber(Supervisable<T> supervisable, IObserver<T> observer) : IEquatable<Subscriber>, IEquatable<IObserver<T>>, IDisposable
	{
		#region 私有变量
		private Supervisable<T> _supervisable = supervisable;
		private IObserver<T> _observer = observer;
		#endregion

		#region 公共属性
		public IObserver<T> Observer => _observer;
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var supervisable = Interlocked.Exchange(ref _supervisable, null);
			if(supervisable != null)
			{
				_observer = null;
				supervisable.Unsubscribe();
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

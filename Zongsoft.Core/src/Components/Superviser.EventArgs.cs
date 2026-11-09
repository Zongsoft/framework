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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Components;

partial class Superviser<T>
{
	public class SupervisedEventArgs : EventArgs
	{
		#region 构造函数
		public SupervisedEventArgs(IObservable<T> observable) : this(null, observable) { }
		public SupervisedEventArgs(object key, IObservable<T> observable)
		{
			this.Key = key;
			this.Observable = observable;
		}
		#endregion

		#region 公共属性
		public object Key { get; }
		public IObservable<T> Observable { get; }
		#endregion
	}

	public class UnsupervisedEventArgs : EventArgs
	{
		#region 构造函数
		public UnsupervisedEventArgs(IObservable<T> observable, SupervisableReason reason) : this(null, observable, reason) { }
		public UnsupervisedEventArgs(object key, IObservable<T> observable, SupervisableReason reason)
		{
			this.Key = key;
			this.Reason = reason;
			this.Observable = observable;
		}
		#endregion

		#region 公共属性
		public object Key { get; }
		public IObservable<T> Observable { get; }
		public SupervisableReason Reason { get; }
		#endregion
	}
}

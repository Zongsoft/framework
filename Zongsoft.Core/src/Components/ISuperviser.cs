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

public interface ISuperviser<T> : IDisposable
{
	#region 事件定义
	event EventHandler<Superviser<T>.SupervisedEventArgs> Supervised;
	event EventHandler<Superviser<T>.UnsupervisedEventArgs> Unsupervised;
	#endregion

	#region 属性定义
	int Count { get; }
	bool IsDisposed { get; }
	bool IsDisposing { get; }

	/// <summary>获取指定键对应的被观察对象。</summary>
	/// <param name="key">指定要获取的被观察对象的键。</param>
	/// <returns>返回指定键所对应的被观察对象，如果为空(<c>null</c>)则表示指定的键不存在。</returns>
	IObservable<T> this[object key] { get; }
	#endregion

	#region 方法定义
	void Clear();
	bool Contains(object key);

	IDisposable Supervise(IObservable<T> observable);
	IDisposable Supervise(object key, IObservable<T> observable);

	bool Unsupervise(object key);
	bool Unsupervise(object key, out IObservable<T> observable);
	#endregion
}

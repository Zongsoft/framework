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

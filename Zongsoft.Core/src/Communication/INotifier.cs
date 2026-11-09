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
using System.Threading.Tasks;

namespace Zongsoft.Communication;

/// <summary>表示通知激发器的接口。</summary>
public interface INotifier
{
	/// <summary>激发一个通知给指定的接受者。</summary>
	/// <param name="name">指定要激发的通知名。</param>
	/// <param name="content">指定的通知内容。</param>
	/// <param name="destination">指定通知的接受者，由具体实现者定义支持的接受者类型。</param>
	/// <param name="settings">指定的通知设置。</param>
	/// <returns>返回通知结果对象，由具体实现者定义。</returns>
	object Notify(string name, object content, object destination, object settings = null);

	/// <summary>异步激发一个通知给指定的接受者。</summary>
	/// <param name="name">指定要激发的通知名。</param>
	/// <param name="content">指定的通知内容。</param>
	/// <param name="destination">指定通知的接受者，由具体实现者定义支持的接受者类型。</param>
	/// <param name="settings">指定的通知设置。</param>
	/// <returns>返回通知结果对象，由具体实现者定义。</returns>
	ValueTask<object> NotifyAsync(string name, object content, object destination, object settings = null);
}

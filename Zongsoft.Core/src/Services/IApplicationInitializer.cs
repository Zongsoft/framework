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

namespace Zongsoft.Services;

/// <summary>提供应用初始化功能的接口。</summary>
public interface IApplicationInitializer
{
	/// <summary>初始化应用扩展模块，并使其为处理请求做好准备。</summary>
	/// <param name="context">一个上下文对象，它提供对模块处理应用程序内所有应用程序对象的公用的方法、属性和事件的访问。</param>
	/// <remarks>使用 <c>Initialize</c> 将事件处理方法向具体事件进行注册等初始化操作。</remarks>
	void Initialize(IApplicationContext context);
}

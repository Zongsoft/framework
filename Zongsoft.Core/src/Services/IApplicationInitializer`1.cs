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
/// <typeparam name="TBuilder">指定的应用构建器的泛型参数。</typeparam>
public interface IApplicationInitializer<TBuilder>
{
	/// <summary>初始化应用扩展模块，并使其为处理请求做好准备。</summary>
	/// <param name="builder">应用构建器对象。</param>
	void Initialize(TBuilder builder);
}

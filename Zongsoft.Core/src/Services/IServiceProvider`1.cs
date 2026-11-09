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

/// <summary>表示特定类型的服务提供程序。</summary>
/// <typeparam name="T">特定的服务类型。</typeparam>
public interface IServiceProvider<out T> where T : class
{
	/// <summary>获取指定名称的服务。</summary>
	/// <param name="name">指定的要获取的服务名称。</param>
	/// <returns>返回指定名称的服务，如果为空(<c>null</c>)则表示指定名称的服务不存在。</returns>
	/// <remarks>对于实现者的要求：当指定名称的服务不存在时，确保返回值为空(<c>null</c>)而不要抛出异常。</remarks>
	T GetService(string name = null);
}

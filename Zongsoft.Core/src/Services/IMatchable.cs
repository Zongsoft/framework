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

/// <summary>提供一种特定于类型的通用匹配方法，某些同类型的类通过实现此接口对其进行更进一步的匹配。</summary>
public interface IMatchable
{
	/// <summary>指示当前对象是否匹配指定参数的条件约束。</summary>
	/// <param name="argument">指定是否匹配逻辑的参数。</param>
	/// <returns>如果当前对象符合<paramref name="argument"/>参数的匹配规则，则为真(<c>True</c>)；否则为假(<c>False</c>)。</returns>
	bool Match(object argument);
}

/// <summary>提供一种特定于类型的通用匹配方法，某些同类型的类通过实现此接口对其进行更进一步的匹配。</summary>
public interface IMatchable<in T>
{
	/// <summary>指示当前对象是否匹配指定参数的条件约束。</summary>
	/// <param name="argument">指定是否匹配逻辑的参数。</param>
	/// <returns>如果当前对象符合<paramref name="argument"/>参数的匹配规则，则为真(<c>True</c>)；否则为假(<c>False</c>)。</returns>
	bool Match(T argument);
}

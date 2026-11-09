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

/// <summary>表示匹配器的接口。</summary>
public interface IMatcher<in T>
{
	/// <summary>判断指定对象是否匹配某种规则。</summary>
	/// <param name="target">指定的待匹配的对象。</param>
	/// <param name="argument">指定的匹配参数。</param>
	/// <returns>如果指定对象匹配成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Match(T target, object argument);
}

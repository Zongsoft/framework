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
using System.Collections.Generic;

namespace Zongsoft.Web;

/// <summary>表示 <see cref="IWebHost"/> 宿主集合的接口。</summary>
public interface IWebHostCollection : ICollection<IWebHost>
{
	/// <summary>获取默认宿主。</summary>
	IWebHost Default { get; }

	/// <summary>获取指定名称的宿主。</summary>
	/// <param name="name">指定的宿主名称。</param>
	/// <returns>返回指定名称的宿主。</returns>
	IWebHost this[string name] { get; }

	/// <summary>尝试获取指定名称的宿主。</summary>
	/// <param name="name">指定要获取的宿主名称。</param>
	/// <param name="value">输出参数，表示指定名称的宿主。</param>
	/// <returns>如果指定名称的宿主获取成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool TryGetValue(string name, out IWebHost value);
}

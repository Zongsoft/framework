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

namespace Zongsoft.Data.Metadata;

/// <summary>提供数据命令内容管理的接口。</summary>
public interface IDataCommandScriptor
{
	IReadOnlySet<string> Drivers { get; }

	/// <summary>获取指定驱动的命令脚本内容。</summary>
	/// <param name="driver">指定要获取的脚本对应的驱动标识名。</param>
	/// <returns>返回脚本内容文本，如果指定的驱动没有对应的脚本则返回空(<c>null</c>)。</returns>
	string GetScript(string driver);

	/// <summary>设置指定驱动的命令脚本内容。</summary>
	/// <param name="driver">指定要设置的脚本对应的驱动标识名。</param>
	/// <param name="text">要设置的脚本内容文本。</param>
	/// <returns>设置成功则返回真；驱动标识为空时返回假。</returns>
	bool SetScript(string driver, string text);
}

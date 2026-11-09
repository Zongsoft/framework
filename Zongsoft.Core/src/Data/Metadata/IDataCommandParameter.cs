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
using System.Data;

namespace Zongsoft.Data.Metadata;

/// <summary>表示命令参数的元数据类。</summary>
public interface IDataCommandParameter
{
	/// <summary>获取命令参数的名称。</summary>
	string Name { get; }

	/// <summary>获取命令参数的别名。</summary>
	string Alias { get; }

	/// <summary>获取命令参数的类型。</summary>
	DataType Type { get; }

	/// <summary>获取命令参数的最大长度。</summary>
	int Length { get; }

	/// <summary>获取或设置命令参数的值。</summary>
	object Value { get; set; }

	/// <summary>获取命令参数的传递方向。</summary>
	ParameterDirection Direction { get; }
}

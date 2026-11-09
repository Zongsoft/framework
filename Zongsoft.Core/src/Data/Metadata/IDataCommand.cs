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

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据命令的元数据类。</summary>
public interface IDataCommand
{
	/// <summary>获取所属命名空间。</summary>
	string Namespace { get; }

	/// <summary>获取数据命令的名称。</summary>
	string Name { get; }

	/// <summary>获取数据命令的限定名称。</summary>
	string QualifiedName { get; }

	/// <summary>获取或设置命令的类型。</summary>
	DataCommandType Type { get; set; }

	/// <summary>获取或设置数据命令的别名（函数或存储过程的名称）。</summary>
	string Alias { get; set; }

	/// <summary>获取或设置命令的变化性。</summary>
	DataCommandMutability Mutability { get; set; }

	/// <summary>获取数据命令的参数集合。</summary>
	DataCommandParameterCollection Parameters { get; }

	/// <summary>获取数据命令的脚本对象。</summary>
	IDataCommandScriptor Scriptor { get; }
}

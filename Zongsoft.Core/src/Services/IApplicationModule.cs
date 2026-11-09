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
using System.Reflection;

using Zongsoft.Collections;

namespace Zongsoft.Services;

/// <summary>表示应用模块（应用子系统）的接口。</summary>
public interface IApplicationModule
{
	/// <summary>获取应用模块名称。</summary>
	string Name { get; }

	/// <summary>获取应用模块版本名。</summary>
	string Edition { get; }

	/// <summary>获取应用模块版本号。</summary>
	Version Version { get; }

	/// <summary>获取应用模块的标题。</summary>
	string Title { get; }

	/// <summary>获取应用模块的描述文本。</summary>
	string Description { get; }

	/// <summary>获取应用模块的程序集。</summary>
	Assembly Assembly { get; }

	/// <summary>获取应用模块的服务容器。</summary>
	IServiceProvider Services { get; }

	/// <summary>获取应用模块的自定义属性集。</summary>
	Parameters Properties { get; }
}

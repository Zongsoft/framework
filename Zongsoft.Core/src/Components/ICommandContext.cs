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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

namespace Zongsoft.Components;

/// <summary>
/// 表示命令执行的上下文的接口。
/// </summary>
public interface ICommandContext
{
	/// <summary>获取当前命令执行器对象。</summary>
	ICommandExecutor Executor { get; }

	/// <summary>获取或设置传入的值。</summary>
	object Value { get; set; }

	/// <summary>获取或设置执行结果。</summary>
	object Result { get; set; }

	/// <summary>获取当前命令执行器的标准输出器。</summary>
	ICommandOutlet Output { get; }

	/// <summary>获取当前命令执行器的错误输出器。</summary>
	TextWriter Error { get; }

	/// <summary>获取当前命令会话的共享参数集。</summary>
	Collections.Parameters Parameters { get; }
}

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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components;

/// <summary>
/// 表示命令执行器的接口。
/// </summary>
public interface ICommandExecutor
{
	#region 声明事件
	event EventHandler<CommandExecutorFailureEventArgs> Failed;
	event EventHandler<CommandExecutorExecutingEventArgs> Executing;
	event EventHandler<CommandExecutorExecutedEventArgs> Executed;
	#endregion

	#region 属性定义
	/// <summary>获取命令执行器的根节点。</summary>
	CommandNode Root { get; }

	/// <summary>获取命令别名映射器。</summary>
	ICommandAliaser Aliaser { get; }

	/// <summary>获取或设置命令表达式解析器。</summary>
	ICommandExpressionParser Parser { get; set; }

	/// <summary>获取或设置命令执行器的标准输出器。</summary>
	ICommandOutlet Output { get; set; }

	/// <summary>获取或设置命令执行器的错误输出器。</summary>
	TextWriter Error { get; set; }

	/// <summary>获取命令执行器的状态信息集。</summary>
	Collections.Parameters States { get; }
	#endregion

	#region 方法定义
	/// <summary>执行命令。</summary>
	/// <param name="expression">指定要执行的命令表达式文本。</param>
	/// <param name="argument">指定的输入参数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回命令执行的结果。</returns>
	ValueTask<object> ExecuteAsync(string expression, object argument = null, CancellationToken cancellation = default);

	/// <summary>查找指定命令路径对应的命令节点。</summary>
	/// <param name="path">指定的命令路径。</param>
	/// <returns>返回指定命令路径对应的命令节点，如果指定的路径不存在则返回空(<c>null</c>)。</returns>
	CommandNode Find(string path);
	#endregion
}

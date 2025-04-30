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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Components;

public class CommandParser : Parser
{
	#region 解析方法
	public override object Parse(ParserContext context) => new DelegateCommand(context.Text);
	#endregion

	#region 嵌套子类
	private class DelegateCommand(string commandText) : CommandBase
	{
		#region 私有变量
		private readonly string _commandText = commandText;
		#endregion

		#region 执行方法
		protected override ValueTask<object> OnExecuteAsync(object parameter, CancellationToken cancellation)
		{
			var commandExecutor = CommandExecutor.Default ??
				throw new InvalidOperationException("Can not get the CommandExecutor from 'Zongsoft.Services.CommandExecutor.Default' static member.");

			return commandExecutor.ExecuteAsync(_commandText, parameter, cancellation);
		}
		#endregion
	}
	#endregion
}

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
				throw new InvalidOperationException(global::Zongsoft.Plugins.Properties.Resources.Command_ExecutorUnavailable_Message);

			return commandExecutor.ExecuteAsync(_commandText, parameter, cancellation);
		}
		#endregion
	}
	#endregion
}

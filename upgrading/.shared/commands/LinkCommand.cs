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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Upgrading.Commands;

[CommandOption(OVERWRITE_OPTION, 'o', typeof(bool), true)]
public class LinkCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly LinkCommand Instance = new();
	#endregion

	#region 常量定义
	private const string OVERWRITE_OPTION = "overwrite";
	#endregion

	#region 构造函数
	public LinkCommand() : base("Link") { }
	public LinkCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty || context.Arguments.Count % 2 != 0)
			return default;

		if(context.Arguments.Count == 2)
		{
			if(File.Exists(context.Arguments[1]))
			{
				if(context.Options.Switch(OVERWRITE_OPTION))
					File.Delete(context.Arguments[0]);

				return ValueTask.FromResult<object>(File.CreateSymbolicLink(context.Arguments[0], context.Arguments[1]));
			}

			return default;
		}

		var result = new FileSystemInfo[context.Arguments.Count / 2];

		for(int i = 0; i < context.Arguments.Count / 2; i++)
		{
			var source = context.Arguments[i * 2];
			var target = context.Arguments[(i * 2) + 1];

			if(File.Exists(target))
			{
				if(context.Options.Switch(OVERWRITE_OPTION))
					File.Delete(source);

				result[i] = File.CreateSymbolicLink(source, target);
			}
		}

		return ValueTask.FromResult<object>(result);
	}
	#endregion
}

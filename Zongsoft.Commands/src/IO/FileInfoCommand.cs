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
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Serialization;

namespace Zongsoft.IO.Commands;

public class FileInfoCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public FileInfoCommand() : base("Info") { }
	public FileInfoCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

		async ValueTask<FileInfo> GetInfoAsync(string path)
		{
			var info = await FileSystem.File.GetInfoAsync(path);

			if(info == null)
				context.Output.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.Text_FileNotExisted, path));
			else
				context.Output.WriteLine(Serializer.Json.Serialize(info, new TextSerializationOptions() { Indented = true }));

			return info;
		}

		if(context.Arguments.Count == 1)
			return await GetInfoAsync(context.Arguments[0]);
		else
			return context.Arguments.Select(async path => await GetInfoAsync(path)).ToArray();
	}
	#endregion
}

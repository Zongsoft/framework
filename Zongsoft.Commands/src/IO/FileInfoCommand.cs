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

using Zongsoft.Services;
using Zongsoft.Serialization;

namespace Zongsoft.IO.Commands
{
	public class FileInfoCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public FileInfoCommand() : base("Info")
		{
		}

		public FileInfoCommand(string name) : base(name)
		{
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Arguments.Length == 0)
				throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

			FileInfo GetInfo(string path)
			{
				var info = FileSystem.File.GetInfo(path);

				if(info == null)
					context.Output.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.Text_FileNotExisted, path));
				else
					context.Output.WriteLine(Serializer.Json.Serialize(info, new TextSerializationOptions() { Indented = true }));

				return info;
			}

			if(context.Expression.Arguments.Length == 1)
				return GetInfo(context.Expression.Arguments[0]);
			else
				return context.Expression.Arguments.Select(path => GetInfo(path)).ToArray();
		}
		#endregion
	}
}

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
using System.IO;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.IO.Commands
{
	[CommandOption(KEY_MODE_OPTION, typeof(FileMode), FileMode.Open, "Text.FileCommand.Options.Mode")]
	[CommandOption(KEY_SHARE_OPTION, typeof(FileShare), FileShare.Read, "Text.FileCommand.Options.Share")]
	[CommandOption(KEY_ACCESS_OPTION, typeof(FileAccess), FileAccess.Read, "Text.FileCommand.Options.Access")]
	public class FileOpenCommand : CommandBase<CommandContext>, ICommandCompletion
	{
		#region 常量定义
		private const string KEY_MODE_OPTION = "mode";
		private const string KEY_SHARE_OPTION = "share";
		private const string KEY_ACCESS_OPTION = "access";
		#endregion

		#region 构造函数
		public FileOpenCommand() : base("Open")
		{
		}

		public FileOpenCommand(string name) : base(name)
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			return FileUtility.OpenFile(context,
				context.Expression.Options.GetValue<FileMode>(KEY_MODE_OPTION),
				context.Expression.Options.GetValue<FileAccess>(KEY_ACCESS_OPTION),
				context.Expression.Options.GetValue<FileShare>(KEY_SHARE_OPTION));
		}
		#endregion

		#region 完成回调
		public void OnCompleted(CommandCompletionContext context)
		{
			if(context.Result is IEnumerable<Stream> streams)
			{
				foreach(var stream in streams)
				{
					if(stream != null)
						stream.Close();
				}
			}
			else if(context.Result is Stream stream)
			{
				stream.Close();
			}
		}
		#endregion
	}
}

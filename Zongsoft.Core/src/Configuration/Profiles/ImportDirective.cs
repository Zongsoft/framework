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

namespace Zongsoft.Configuration.Profiles;

/// <summary>按声明文件解析路径并将指定来源导入当前配置。</summary>
public sealed class ImportDirective() : ProfileDirectiveBase(ProfileDirectiveOptions.ImportOptions.NAME)
{
	#region 单例字段
	public static readonly ImportDirective Instance = new();
	#endregion

	#region 重写方法
	/// <summary>执行当前导入指令，共享调用方的读取会话。</summary>
	/// <param name="context">本次指令上下文。</param>
	public override void Process(ProfileDirectiveContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		if(string.IsNullOrEmpty(context.Argument))
			return;

		var maximumDepth = context.Configuration is ProfileDirectiveOptions.ImportOptions { MaximumDepth: > 0 } options ? options.MaximumDepth : ProfileDirectiveOptions.ImportOptions.DEFAULT_MAXIMUM_DEPTH;

		foreach(var argument in context.Argument.Split([' ', '\t', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
		{
			var path = argument;

			if(!Path.IsPathFullyQualified(path))
			{
				if(string.IsNullOrEmpty(context.FilePath))
					throw new ProfileException(string.Format(Properties.Resources.Profiles_RelativeImportRequiresFile_Message, path, context.LineNumber));

				path = Path.Combine(Path.GetDirectoryName(context.FilePath), path);
			}

			path = Path.GetFullPath(path);
			FileStream stream;

			//仅打开阶段允许忽略缺失；读取及回调中的同类异常仍须传播。
			try
			{
				stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			}
			catch(IOException exception) when(exception is FileNotFoundException or DirectoryNotFoundException)
			{
				if(context.Behavior == ProfileDirectiveBehavior.Strict)
					throw new ProfileException(string.Format(Properties.Resources.Profiles_RequiredImport_Message, path, context.FilePath, context.LineNumber), exception);

				continue;
			}

			//共享当前读取会话；会话拥有流并在成功或失败时释放。
			context.Read(stream, maximumDepth);
		}
	}
	#endregion
}

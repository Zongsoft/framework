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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Upgrading.Commands;

[CommandOption(OVERWRITE_OPTION, 'o', typeof(bool), true)]
public class CopyCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly CopyCommand Instance = new();
	#endregion

	#region 常量定义
	private const string OVERWRITE_OPTION = "overwrite";
	#endregion

	#region 构造函数
	public CopyCommand() : base("Copy") { }
	public CopyCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count == 2)
		{
			var source = context.Arguments[0];
			var destination = context.Arguments[1];

			if(Path.EndsInDirectorySeparator(source) && Directory.Exists(source))
			{
				//确保目标目录存在，如果不存在则创建它
				EnsureDirectory(destination);

				foreach(var path in Directory.EnumerateFileSystemEntries(source, "*", SearchOption.AllDirectories))
				{
					if(Path.EndsInDirectorySeparator(path))
						EnsureDirectory(Path.Combine(destination, path[source.Length..]));
					else
						File.Copy(path, Path.Combine(destination, path[source.Length..]), context.Options.Switch(OVERWRITE_OPTION));
				}
			}
			else
				File.Copy(context.Arguments[0], context.Arguments[1], context.Options.Switch(OVERWRITE_OPTION));
		}

		return default;
	}
	#endregion

	#region 私有方法
	static DirectoryInfo EnsureDirectory(string path)
	{
		var directory = new DirectoryInfo(path);
		return directory.Exists ? directory : Directory.CreateDirectory(path);
	}
	#endregion
}

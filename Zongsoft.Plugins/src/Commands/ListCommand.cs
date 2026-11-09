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
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Plugins.Commands;

[DisplayName("Text.ListCommand.Name")]
[Description("Text.ListCommand.Description")]
public class ListCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private readonly PluginTree _pluginTree;
	#endregion

	#region 构造函数
	public ListCommand(PluginTree pluginTree) : this("List", pluginTree) { }
	public ListCommand(string name, PluginTree pluginTree) : base(name)
	{
		_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		int index = 0;

		foreach(var plugin in _pluginTree.Plugins)
			DumpPlugin(context.Output, plugin, 0, index++);

		return ValueTask.FromResult<object>(_pluginTree.Plugins);
	}
	#endregion

	#region 私有方法
	private static void DumpPlugin(ICommandOutlet output, Plugin plugin, int depth, int index)
	{
		if(plugin == null)
			return;

		var indent = depth > 0 ? new string('\t', depth) : string.Empty;
		var content = CommandOutletContent
			.Create(CommandOutletColor.DarkMagenta, $"{indent}[{(index + 1)}] ")
			.Append(plugin.Name);

		if(plugin.Manifest.Version.Major > 0 || plugin.Manifest.Version.Minor > 0 || plugin.Manifest.Version.Build > 0 || plugin.Manifest.Version.Revision > 0)
		{
			content.Last
				.Append(CommandOutletColor.DarkGray, "@")
				.Append(CommandOutletColor.Blue, plugin.Manifest.Version.ToString());
		}

		if(plugin.IsMaster)
			content.Last.AppendLine(CommandOutletColor.DarkCyan, " (master)");
		else
			content.Last.AppendLine();

		content.Last.Append(indent);
		var directoryName = GetCurrentDirectoryName(plugin.FilePath);

		if(!string.IsNullOrEmpty(directoryName))
		{
			content.Last
				.Append(CommandOutletColor.DarkGreen, directoryName)
				.Append('/');
		}

		content.Last.Append(CommandOutletColor.DarkYellow, Path.GetFileName(plugin.FilePath));

		if(File.Exists(plugin.FilePath))
		{
			var fileInfo = new FileInfo(plugin.FilePath);
			content.Last.Append(CommandOutletColor.DarkGray, $" [{fileInfo.LastWriteTime}]");
		}

		output.WriteLine(content);

		if(plugin.Children.Count > 0)
		{
			var childIndex = 0;

			foreach(var child in plugin.Children)
				DumpPlugin(output, child, depth + 1, childIndex++);
		}
	}

	private static string GetCurrentDirectoryName(string filePath)
	{
		if(string.IsNullOrWhiteSpace(filePath))
			return string.Empty;

		var directoryPath = Path.GetDirectoryName(filePath);
		var index = directoryPath.LastIndexOf(Path.DirectorySeparatorChar);

		if(index > 0 && index < directoryPath.Length - 1)
			return directoryPath[(index + 1)..];

		return string.Empty;
	}
	#endregion
}

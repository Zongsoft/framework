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
using System.IO;
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Plugins.Commands
{
	[DisplayName("Text.ListCommand.Name")]
	[Description("Text.ListCommand.Description")]
	public class ListCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private readonly PluginTree _pluginTree;
		#endregion

		#region 构造函数
		public ListCommand(PluginTree pluginTree) : this("List", pluginTree)
		{
		}

		public ListCommand(string name, PluginTree pluginTree) : base(name)
		{
			_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			int index = 0;

			foreach(var plugin in _pluginTree.Plugins)
				WritePlugin(context.Output, plugin, 0, index++);

			return _pluginTree.Plugins;
		}
		#endregion

		#region 私有方法
		private static void WritePlugin(ICommandOutlet output, Plugin plugin, int depth, int index)
		{
			if(plugin == null)
				return;

			var indent = depth > 0 ? new string('\t', depth) : string.Empty;
			var content = CommandOutletContent.Create(CommandOutletColor.DarkMagenta, $"{indent}[{(index + 1)}] ").Append(plugin.Name);

			if(plugin.Manifest.Version.Major > 0 || plugin.Manifest.Version.Minor > 0 || plugin.Manifest.Version.Build > 0 || plugin.Manifest.Version.Revision > 0)
			{
				content.Append(CommandOutletColor.DarkGray, "@")
				       .Append(CommandOutletColor.Blue, plugin.Manifest.Version.ToString());
			}

			if(plugin.IsMaster)
				content.AppendLine(CommandOutletColor.DarkCyan, " (master)");
			else
				content.AppendLine();

			content.Append(indent);
			var directoryName = GetCurrentDirectoryName(plugin.FilePath);

			if(!string.IsNullOrEmpty(directoryName))
			{
				content.Append(CommandOutletColor.DarkGreen, directoryName);
				content.Append("/");
			}

			content.Append(CommandOutletColor.DarkYellow, Path.GetFileName(plugin.FilePath));

			if(File.Exists(plugin.FilePath))
			{
				var fileInfo = new FileInfo(plugin.FilePath);
				content.Append(CommandOutletColor.DarkGray, $" [{fileInfo.LastWriteTime}]");
			}

			output.WriteLine(content);

			if(plugin.Children.Count > 0)
			{
				var childIndex = 0;

				foreach(var child in plugin.Children)
					WritePlugin(output, child, depth + 1, childIndex++);
			}
		}

		private static string GetCurrentDirectoryName(string filePath)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				return string.Empty;

			var directoryPath = Path.GetDirectoryName(filePath);
			var index = directoryPath.LastIndexOf(Path.DirectorySeparatorChar);

			if(index > 0 && index < directoryPath.Length - 1)
				return directoryPath.Substring(index + 1);

			return string.Empty;
		}
		#endregion
	}
}

/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
		private PluginContext _pluginContext;
		#endregion

		#region 构造函数
		public ListCommand(PluginContext pluginContext) : this("List", pluginContext)
		{
		}

		public ListCommand(string name, PluginContext pluginContext) : base(name)
		{
			_pluginContext = pluginContext ?? throw new ArgumentNullException(nameof(pluginContext));
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			int index = 0;

			foreach(var plugin in _pluginContext.Plugins)
			{
				this.WritePlugin(context.Output, plugin, 0, index++);
			}

			return _pluginContext.Plugins;
		}
		#endregion

		#region 私有方法
		private void WritePlugin(ICommandOutlet output, Plugin plugin, int depth, int index)
		{
			if(plugin == null)
				return;

			var indent = depth > 0 ? new string('\t', depth) : string.Empty;

			output.Write(CommandOutletColor.DarkMagenta, indent + "[{0}] ", index + 1);
			output.Write(plugin.Name);

			if(plugin.IsMaster)
				output.WriteLine(CommandOutletColor.DarkCyan, " (Master)");
			else
				output.WriteLine();

			output.Write(indent);

			var directoryName = this.GetCurrentDirectoryName(plugin.FilePath);

			if(!string.IsNullOrEmpty(directoryName))
			{
				output.Write(CommandOutletColor.DarkGreen, "{0}", directoryName);
				output.Write("/");
			}

			output.Write(CommandOutletColor.DarkYellow, Path.GetFileName(plugin.FilePath));

			if(System.IO.File.Exists(plugin.FilePath))
			{
				var fileInfo = new System.IO.FileInfo(plugin.FilePath);
				output.WriteLine(CommandOutletColor.DarkGray, " [{0}]", fileInfo.LastWriteTime);
			}
			else
			{
				output.WriteLine();
			}

			if(plugin.Children.Count > 0)
			{
				var childIndex = 0;

				foreach(var child in plugin.Children)
				{
					this.WritePlugin(output, child, depth + 1, childIndex);
				}

				output.WriteLine();
			}
		}

		private string GetCurrentDirectoryName(string filePath)
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

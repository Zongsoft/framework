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

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Serialization;

namespace Zongsoft.Plugins.Commands;

internal static class Utility
{
	public static CommandOutletContent GetPluginNodeContent(PluginTreeNode node, ObtainMode obtainMode, int maxDepth)
	{
		if(node == null)
			return null;

		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.DarkCyan, node.NodeType.ToString())
			.Append(CommandOutletColor.DarkGray, "]")
			.Append(CommandOutletColor.DarkGreen, node.FullPath);

		if(node.Plugin == null)
			content.AppendLine();
		else
			content.Append(CommandOutletColor.DarkGray, "@")
			       .AppendLine(CommandOutletColor.DarkCyan, node.Plugin.Name);

		if(node.Properties.Count > 0)
		{
			foreach(PluginExtendedProperty property in node.Properties)
			{
				content
					.Append(CommandOutletColor.DarkCyan, property.Name)
					.Append(CommandOutletColor.DarkGray, "=")
					.Append(CommandOutletColor.DarkYellow, property.RawValue);

				if(property.Value != null)
				{
					content.Append(CommandOutletColor.DarkGray, " [");
					content.Append(CommandOutletColor.DarkMagenta, property.Value.GetType().GetAlias());
					content.Append(CommandOutletColor.DarkGray, "]");
				}

				content.AppendLine();
			}
		}

		if(node.Children.Count > 0)
		{
			content.AppendLine();

			foreach(var child in node.Children)
			{
				content
					.Append(CommandOutletColor.DarkGray, "[")
					.Append(CommandOutletColor.DarkCyan, child.NodeType.ToString())
					.Append(CommandOutletColor.DarkGray, "]")
					.Append(CommandOutletColor.DarkGreen, child.Name);

				if(child.Plugin == null)
					content.AppendLine();
				else
					content.Append(CommandOutletColor.DarkGray, "@")
					       .AppendLine(CommandOutletColor.DarkCyan, child.Plugin.Name);
			}
		}

		object value = node.UnwrapValue(obtainMode);

		if(value != null)
		{
			try
			{
				content.Dump(value);
			}
			catch(Exception ex)
			{
				content
					.Append(CommandOutletColor.Red, ex.GetType().GetAlias())
					.Append(CommandOutletColor.Gray, ":")
					.AppendLine(CommandOutletColor.Yellow, ex.Message);
			}
		}

		return content;
	}
}

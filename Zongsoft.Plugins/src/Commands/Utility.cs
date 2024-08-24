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
using System.Text.Json;
using System.Text.Json.Serialization;

using Zongsoft.Services;

namespace Zongsoft.Plugins.Commands
{
	internal static class Utility
	{
		public static void PrintPluginNode(ICommandOutlet output, PluginTreeNode node, ObtainMode obtainMode, int maxDepth)
		{
			if(node == null)
				return;

			output.Write(CommandOutletColor.DarkYellow, "[{0}]", node.NodeType);
			output.WriteLine(node.FullPath);
			output.Write(CommandOutletColor.DarkYellow, "Plugin File: ");

			if(node.Plugin == null)
				output.WriteLine(CommandOutletColor.Red, "N/A");
			else
				output.WriteLine(node.Plugin.FilePath);

			output.Write(CommandOutletColor.DarkYellow, "Node Properties: ");
			output.WriteLine(node.Properties.Count);

			if(node.Properties.Count > 0)
			{
				output.WriteLine(CommandOutletColor.Gray, "{");

				foreach(PluginExtendedProperty property in node.Properties)
				{
					output.Write(CommandOutletColor.DarkYellow, "\t" + property.Name);
					output.Write(" = ");
					output.Write(property.RawValue);

					if(property.Value != null)
					{
						output.Write(CommandOutletColor.DarkGray, " [");
						output.Write(CommandOutletColor.Blue, property.Value.GetType().FullName);
						output.Write(CommandOutletColor.DarkGray, "]");
					}

					output.WriteLine();
				}

				output.WriteLine(CommandOutletColor.Gray, "}");
			}

			output.WriteLine(CommandOutletColor.DarkYellow, "Children: {0}", node.Children.Count);
			if(node.Children.Count > 0)
			{
				output.WriteLine();

				foreach(var child in node.Children)
				{
					output.WriteLine(child);
				}
			}

			object value = node.UnwrapValue(obtainMode);
			if(value != null)
			{
				var json = JsonSerializer.Serialize(value, new JsonSerializerOptions()
				{
					MaxDepth = maxDepth,
					WriteIndented = true,
					ReferenceHandler = ReferenceHandler.Preserve,
				});

				output.WriteLine();
				output.WriteLine(json);
			}
		}
	}
}

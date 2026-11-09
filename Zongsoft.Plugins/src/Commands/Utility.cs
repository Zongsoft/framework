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

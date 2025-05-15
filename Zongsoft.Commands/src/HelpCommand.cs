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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Commands;

[DisplayName("Text.HelpCommand.Name")]
[Description("Text.HelpCommand.Description")]
public class HelpCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public HelpCommand() : base("Help") { }
	public HelpCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Expression.Arguments.Length == 0)
		{
			PrintApplication(context.Output);

			foreach(var node in context.Executor.Root.Children)
				PrintCommandNode(context.Output, node, 0);

			return ValueTask.FromResult<object>(null);
		}

		foreach(var argument in context.Expression.Arguments)
		{
			if(argument == "?" || argument == ".")
			{
				PrintHelpInfo(context.Output, this);
				continue;
			}

			CommandTreeNode node = context.Executor.Find(argument);

			if(node == null)
			{
				context.Output.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.Text_Message_CommandNotFound, argument));
				continue;
			}

			if(node != null && node.Command != null)
			{
				context.Output.WriteLine(node.FullPath);
				PrintHelpInfo(context.Output, node.Command);
			}
		}

		return ValueTask.FromResult<object>(null);
	}
	#endregion

	#region 静态方法
	public static void PrintApplication(ICommandOutlet output)
	{
		var application = ApplicationContext.Current;
		if(application == null)
			return;

		//打印应用名称(应用名称不会为空)
		output.WriteLine(CommandOutletColor.Green, application.Name);

		if(!string.IsNullOrWhiteSpace(application.Title))
			output.WriteLine(CommandOutletColor.DarkGreen, application.Title);
		if(!string.IsNullOrWhiteSpace(application.Description))
			output.WriteLine(CommandOutletColor.DarkGray, application.Description);

		//打印分隔行
		output.WriteLine();
	}

	public static void PrintHelpInfo(ICommandOutlet output, ICommand command)
	{
		if(output == null || command == null)
			return;

		DisplayNameAttribute displayName = (DisplayNameAttribute)TypeDescriptor.GetAttributes(command)[typeof(DisplayNameAttribute)];
		DescriptionAttribute description = (DescriptionAttribute)TypeDescriptor.GetAttributes(command)[typeof(DescriptionAttribute)];

		output.Write(CommandOutletColor.Blue, command.Name + " ");

		if(!command.Enabled)
			output.Write(CommandOutletColor.DarkGray, "({0})", Properties.Resources.Text_Disabled);

		if(displayName == null || string.IsNullOrWhiteSpace(displayName.DisplayName))
			output.Write(Properties.Resources.Text_Command);
		else
			output.Write(GetResourceString(displayName.DisplayName, command.GetType().Assembly));

		CommandOptionAttribute[] optionAttributes = (CommandOptionAttribute[])command.GetType().GetCustomAttributes(typeof(CommandOptionAttribute), true);

		if(optionAttributes != null && optionAttributes.Length > 0)
		{
			output.WriteLine("," + Properties.Resources.Text_CommandUsages, optionAttributes.Length);
			output.WriteLine();

			string commandName = command.Name;

			output.Write(CommandOutletColor.Blue, commandName + " ");

			foreach(var optionAttribute in optionAttributes)
			{
				if(optionAttribute.Required)
				{
					output.Write("<-");
					output.Write(CommandOutletColor.DarkYellow, optionAttribute.Name);
					output.Write("> ");
				}
				else
				{
					output.Write("[-");
					output.Write(CommandOutletColor.DarkYellow, optionAttribute.Name);
					output.Write("] ");
				}
			}

			output.WriteLine();

			int maxOptionLength = GetMaxOptionLength(optionAttributes) + 2;

			foreach(var optionAttribute in optionAttributes)
			{
				int optionPadding = maxOptionLength - optionAttribute.Name.Length;

				output.Write("\t-");
				output.Write(CommandOutletColor.DarkYellow, optionAttribute.Name);

				if(optionAttribute.Type != null)
				{
					output.Write(":");
					output.Write(CommandOutletColor.Magenta, GetSimpleTypeName(optionAttribute.Type));
					optionPadding -= (GetSimpleTypeName(optionAttribute.Type).Length + 1);
				}

				output.Write(" (".PadLeft(optionPadding));

				if(optionAttribute.Required)
					output.Write(CommandOutletColor.DarkRed, Properties.Resources.Text_Required);
				else
					output.Write(CommandOutletColor.DarkGreen, Properties.Resources.Text_Optional);

				output.Write(") ");

				if(!string.IsNullOrWhiteSpace(optionAttribute.Description))
					output.Write(GetResourceString(optionAttribute.Description, command.GetType().Assembly));

				if(optionAttribute.Type != null && optionAttribute.Type.IsEnum)
				{
					var entries = Zongsoft.Common.EnumUtility.GetEnumEntries(optionAttribute.Type, false);
					var maxEnumLength = entries.Max(entry => entry.HasAliases ? entry.Name.Length + entry.Aliases.Sum(alias => alias.Length) + entry.Aliases.Length + 1 : entry.Name.Length);

					foreach(var entry in entries)
					{
						var enumPadding = maxEnumLength - entry.Name.Length;

						output.WriteLine();
						output.Write("\t".PadRight(optionAttribute.Name.Length + 3));
						output.Write(CommandOutletColor.DarkMagenta, entry.Name.ToLowerInvariant());

						if(entry.HasAliases)
						{
							var alias = string.Join(',', entry.Aliases);

							output.Write(CommandOutletColor.DarkGray, "(");
							output.Write(CommandOutletColor.DarkMagenta, alias);
							output.Write(CommandOutletColor.DarkGray, ")");

							enumPadding -= alias.Length + 2;
						}

						if(!string.IsNullOrWhiteSpace(entry.Description))
							output.Write(new string(' ', enumPadding + 1) + entry.Description);
					}
				}

				output.WriteLine();
			}
		}

		if(description != null && !string.IsNullOrWhiteSpace(description.Description))
		{
			output.WriteLine();
			output.WriteLine(CommandOutletColor.DarkYellow, GetResourceString(description.Description, command.GetType().Assembly));
		}

		output.WriteLine();
	}
	#endregion

	#region 私有方法
	private static void PrintCommandNode(ICommandOutlet output, CommandTreeNode node, int depth)
	{
		if(node == null)
			return;

		var indent = depth > 0 ? new string(' ', depth * 4) : string.Empty;
		var fulName = node.FullPath.Trim('/').Replace('/', '.');

		if(node.Command == null)
			output.WriteLine("{1}[{0}]", fulName, indent);
		else
		{
			var displayName = (DisplayNameAttribute)Attribute.GetCustomAttribute(node.Command.GetType(), typeof(DisplayNameAttribute), true);

			output.Write("{1}{0}", fulName, indent);

			if(displayName == null)
				output.WriteLine();
			else
				output.WriteLine(CommandOutletColor.DarkYellow, " " + GetResourceString(displayName.DisplayName, node.Command.GetType().Assembly));
		}

		if(node.Children.Count > 0)
		{
			foreach(var child in node.Children)
				PrintCommandNode(output, child, depth + 1);
		}
	}

	private static int GetMaxOptionLength(IEnumerable<CommandOptionAttribute> attributes)
	{
		if(attributes == null)
			return 0;

		int result = 0;

		foreach(var attribute in attributes)
		{
			if(attribute.Type == null)
				result = Math.Max(attribute.Name.Length, result);
			else
				result = Math.Max(attribute.Name.Length + GetSimpleTypeName(attribute.Type).Length + 1, result);
		}

		return result > 0 ? result + 1 : result;
	}

	private static string GetSimpleTypeName(Type type)
	{
		if(type.IsEnum)
			return "enum";

		return Type.GetTypeCode(type) switch
		{
			TypeCode.Boolean => "boolean",
			TypeCode.Byte => "byte",
			TypeCode.Char => "char",
			TypeCode.DateTime => "datetime",
			TypeCode.Decimal or TypeCode.Double => "numeric",
			TypeCode.Int16 or TypeCode.UInt16 => "short",
			TypeCode.Int32 or TypeCode.UInt32 => "int",
			TypeCode.Int64 or TypeCode.UInt64 => "long",
			TypeCode.String => "string",
			_ => TypeAlias.GetAlias(type),
		};
	}

	private static string GetResourceString(string name, Assembly assembly)
	{
		var names = assembly.GetManifestResourceNames();

		for(int i = 0; i < names.Length; i++)
		{
			using var stream = assembly.GetManifestResourceStream(names[i]);
			using var resource = new System.Resources.ResourceSet(stream);

			var value = resource.GetString(name);

			if(value != null)
				return value;
		}

		return name;
	}
	#endregion
}

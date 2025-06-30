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
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Reflection;
using Zongsoft.Collections;

namespace Zongsoft.Components;

public static class CommandOutletDumper
{
	#region 静态属性
	public static ICollection<ICommandOutletDumper> Dumpers => [];
	#endregion

	#region 公共方法
	public static CommandOutletContent Dump(object target, int indent = 0) => Dump(target, null, indent);
	public static CommandOutletContent Dump(object target, CommandOutletDumperOptions options, int indent = 0) => Dump((CommandOutletContent)null, target, options, indent);

	public static CommandOutletContent Dump(this ICommandOutlet output, object target, int indent = 0) => Dump(output, target, null, indent);
	public static CommandOutletContent Dump(this ICommandOutlet output, object target, CommandOutletDumperOptions options, int indent = 0)
	{
		var content = Dump(target, options, indent);
		output.Write(content);
		return content;
	}

	public static CommandOutletContent Dump(this CommandOutletContent content, object target, int indent = 0) => Dump(content, target, null, indent);
	public static CommandOutletContent Dump(this CommandOutletContent content, object target, CommandOutletDumperOptions options, int indent = 0)
	{
		if(target == null)
			return content;

		content ??= CommandOutletContent.Create();
		DumpValue(content, options, target, indent);
		return content;
	}
	#endregion

	#region 私有方法
	private static void DumpValue(CommandOutletContent content, CommandOutletDumperOptions options, object value, int indent)
	{
		options ??= new CommandOutletDumperOptions();

		//获取当前对象的输出器
		var dumper = options.Selector?.Invoke(value);
		if(dumper != null)
		{
			dumper.Dump(content, value, options, indent);
			return;
		}

		switch(value)
		{
			case null:
				content.Last.AppendLine(CommandOutletColor.Gray, "NULL");
				break;
			case Type type:
				content.Last.AppendLine(type.GetAlias(true));
				break;
			case Assembly assembly:
				content.Last
					.Append(assembly.GetName().Name)
					.Append(CommandOutletColor.DarkGray, "@")
					.Append(CommandOutletColor.DarkGreen, assembly.GetName().Version.ToString());

				if(!string.IsNullOrEmpty(assembly.Location))
					content.Last
						.Append(CommandOutletColor.Gray, "(")
						.Append(CommandOutletColor.DarkYellow, assembly.Location)
						.Append(CommandOutletColor.Gray, ")");

				if(assembly.IsDynamic)
					content.Last
						.Append(CommandOutletColor.DarkGray, "(")
						.Append(CommandOutletColor.DarkMagenta, "dynamic")
						.Append(CommandOutletColor.DarkGray, ")");

				content.Last.AppendLine();

				break;
			case byte[] binary:
				content.Last.AppendLine(CommandOutletColor.DarkYellow, System.Convert.ToHexString(binary));
				break;
			case string @string:
				DumpString(content, @string);
				break;
			case DateTime datetime:
				DumpString(content, datetime.Kind == DateTimeKind.Utc ? new DateTimeOffset(datetime.ToLocalTime()).ToString() : datetime.ToString("yyyy-MM-dd HH:mm:ss"));
				break;
			case StringBuilder builder:
				DumpString(content, builder.ToString());
				break;
			case IEnumerable items:
				content.Last.AppendLine();
				content.Last.Indent(options, indent);
				content.Last.AppendLine(CommandOutletColor.Magenta, "{");

				foreach(var item in items)
				{
					if(DictionaryUtility.TryGetEntry(item, out var entryKey, out var entryValue))
					{
						content.Last
							.Append(CommandOutletColor.DarkYellow, entryKey.ToString())
							.Append(CommandOutletColor.DarkGray, "=");

						DumpValue(content, options, entryValue, indent + 1);
					}
					else
						DumpValue(content, options, item, indent + 1);
				}

				content.Last.Indent(options, indent);
				content.Last.AppendLine(CommandOutletColor.Magenta, "}");
				break;
			case Delegate function:
				if(function.Target == null)
					content.Last.Append(CommandOutletColor.Magenta, "static ");

				content.Last
					.Append(CommandOutletColor.Cyan, function.Method.DeclaringType.GetAlias(true))
					.Append(CommandOutletColor.White, ':')
					.Append(CommandOutletColor.DarkGreen, function.Method.Name);

				if(function.Method.IsGenericMethod)
				{
					content.Last.Append(CommandOutletColor.DarkYellow, '<');

					var types = function.Method.GetGenericArguments();
					for(int i = 0; i < types.Length; i++)
					{
						if(i > 0)
							content.Last.Append(CommandOutletColor.DarkGray, ", ");

						content.Last.Append(types[i].GetAlias(true));
					}

					content.Last.Append(CommandOutletColor.DarkYellow, '>');
				}

				content.Last.Append(CommandOutletColor.DarkGray, '(');

				if(function.Method.GetParameters().Length > 0)
					content.Last.Append(CommandOutletColor.Gray, "...");

				content.Last.AppendLine(CommandOutletColor.DarkGray, ')');

				break;
			default:
				DumpObject(content, options, value, indent);
				break;
		}
	}

	private static void DumpString(CommandOutletContent content, string value)
	{
		if(value == null)
		{
			content.Last.AppendLine(CommandOutletColor.Gray, "NULL");
			return;
		}

		if(string.IsNullOrWhiteSpace(value))
			content.Last
				.Append(CommandOutletColor.DarkGray, "<")
				.Append(CommandOutletColor.Yellow, "Empty")
				.AppendLine(CommandOutletColor.DarkGray, ">");
		else
			content.Last.AppendLine(CommandOutletColor.DarkGreen, value);
	}

	private static void DumpObject(CommandOutletContent content, CommandOutletDumperOptions options, object value, int indent)
	{
		if(Common.Convert.TryConvertValue<string>(value, out var text))
		{
			DumpString(content, text);
			return;
		}

		content.Last.AppendLine();
		var members = options.GetMembers(value);

		for(int i = 0; i < members.Length; i++)
		{
			switch(members[i])
			{
				case FieldInfo field:
					if(Reflector.TryGetValue(field, ref value, out var fieldValue))
						DumpMember(content, options, fieldValue, field.Name, field.FieldType, indent + 1);

					break;
				case PropertyInfo property:
					if(property.IsIndexer())
						break;

					if(Reflector.TryGetValue(property, ref value, out var propertyValue))
						DumpMember(content, options, propertyValue, property.Name, property.PropertyType, indent + 1);

					break;
				default:
					break;
			}
		}
	}

	private static void DumpMember(CommandOutletContent content, CommandOutletDumperOptions options, object value, string memberName, Type memberType, int indent)
	{
		content.Indent(options, indent);

		content.Last
			.Append(CommandOutletColor.DarkCyan, memberName)
			.Append(CommandOutletColor.DarkGray, ":")
			.Append(CommandOutletColor.DarkBlue, "(");

		if(memberType.IsEnum)
			content.Last
				.Append(CommandOutletColor.Magenta, "ENUM")
				.Append(CommandOutletColor.DarkGray, "!")
				.Append(CommandOutletColor.DarkYellow, memberType.Name);
		else
			content.Last.Append(CommandOutletColor.DarkYellow, memberType.GetAlias(true));

		content.Last.Append(CommandOutletColor.DarkBlue, ") ");

		var trackable = options.Tracker.CanTrack(value);

		if(trackable)
		{
			bool tracked = false;

			try
			{
				if(tracked = options.Tracker.Track(value))
					DumpValue(content, options, value, indent);
				else
					content.Last
						.Append(CommandOutletColor.DarkGray, "<")
						.Append(CommandOutletColor.DarkRed, "Circular Reference")
						.AppendLine(CommandOutletColor.DarkGray, ">");
			}
			finally
			{
				if(tracked)
					options.Tracker.Untrack();
			}
		}
		else
			DumpValue(content, options, value, indent);
	}

	private static CommandOutletContent Indent(this CommandOutletContent content, CommandOutletDumperOptions options, int indent)
	{
		if(indent > 0)
		{
			var text = options.Indent(indent);

			if(!string.IsNullOrEmpty(text))
				content.Last.Append(text);
		}

		return content;
	}
	#endregion
}

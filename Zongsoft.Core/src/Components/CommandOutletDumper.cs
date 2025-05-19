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
	#region 私有常量
	private const string INDENT_SYMBOL = "    ";
	private const BindingFlags BINDING = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetField | BindingFlags.GetProperty;
	#endregion

	#region 公共方法
	public static CommandOutletContent Dump(object instance, int indent = 0) => Dump(instance, BINDING, indent);
	public static CommandOutletContent Dump(object instance, BindingFlags binding, int indent = 0) => Dump((CommandOutletContent)null, instance, binding, indent);

	public static CommandOutletContent Dump(this ICommandOutlet output, object instance, int indent = 0) => Dump(output, instance, BINDING, indent);
	public static CommandOutletContent Dump(this ICommandOutlet output, object instance, BindingFlags binding, int indent = 0)
	{
		var content = Dump(instance, binding, indent);
		output.Write(content);
		return content;
	}

	public static CommandOutletContent Dump(this CommandOutletContent content, object instance, int indent = 0) => Dump(content, instance, BINDING, indent);
	public static CommandOutletContent Dump(this CommandOutletContent content, object instance, BindingFlags binding, int indent = 0)
	{
		if(instance == null)
			return content;

		content ??= CommandOutletContent.Create();
		DumpValue(new Tracker(), content, instance, binding, indent);
		return content;
	}
	#endregion

	#region 私有方法
	private static void DumpValue(Tracker tracker, CommandOutletContent content, object value, BindingFlags binding, int indent)
	{
		if(indent > 10)
			return;

		switch(value)
		{
			case null:
				content.AppendLine(CommandOutletColor.Gray, "NULL");
				break;
			case Type type:
				content.AppendLine(type.GetAlias());
				break;
			case Assembly assembly:
				content
					.Append(assembly.GetName().Name)
					.Append(CommandOutletColor.DarkGray, "@")
					.Append(CommandOutletColor.DarkGreen, assembly.GetName().Version.ToString());

				if(!string.IsNullOrEmpty(assembly.Location))
					content
						.Append(CommandOutletColor.Gray, "(")
						.Append(CommandOutletColor.DarkYellow, assembly.Location)
						.Append(CommandOutletColor.Gray, ")");

				if(assembly.IsDynamic)
					content
						.Append(CommandOutletColor.DarkGray, "(")
						.Append(CommandOutletColor.DarkMagenta, "dynamic")
						.Append(CommandOutletColor.DarkGray, ")");

				content.AppendLine();

				break;
			case byte[] binary:
				content.AppendLine(CommandOutletColor.DarkYellow, System.Convert.ToHexString(binary));
				break;
			case string @string:
				DumpString(content, @string);
				break;
			case StringBuilder builder:
				DumpString(content, builder.ToString());
				break;
			case IEnumerable items:
				content.AppendLine();
				content.Indent(indent);
				content.AppendLine(CommandOutletColor.Magenta, "{");

				foreach(var item in items)
				{
					if(DictionaryUtility.TryGetEntry(item, out var entryKey, out var entryValue))
					{
						content
							.Append(CommandOutletColor.DarkYellow, entryKey.ToString())
							.Append(CommandOutletColor.DarkGray, "=");

						DumpValue(tracker, content, entryValue, binding, indent + 1);
					}
					else
						DumpValue(tracker, content, item, binding, indent + 1);
				}

				content.Indent(indent);
				content.AppendLine(CommandOutletColor.Magenta, "}");
				break;
			default:
				if(Common.Convert.TryConvertValue<string>(value, out var text))
				{
					DumpString(content, text);
					return;
				}

				content.AppendLine();
				var members = value.GetType().GetMembers(binding);

				for(int i = 0; i < members.Length; i++)
				{
					switch(members[i])
					{
						case FieldInfo field:
							var fieldValue = Reflector.GetValue(field, ref value);
							DumpMember(tracker, content, fieldValue, field.Name, field.FieldType, binding, indent + 1);

							break;
						case PropertyInfo property:
							if(property.IsIndexer())
								break;

							var propertyValue = Reflector.GetValue(property, ref value);
							DumpMember(tracker, content, propertyValue, property.Name, property.PropertyType, binding, indent + 1);

							break;
						default:
							break;
					}
				}

				break;
		}
	}

	private static void DumpString(CommandOutletContent content, string value)
	{
		if(value == null)
		{
			content.AppendLine(CommandOutletColor.Gray, "NULL");
			return;
		}

		if(string.IsNullOrWhiteSpace(value))
			content
				.Append(CommandOutletColor.DarkGray, "<")
				.Append(CommandOutletColor.Yellow, "Empty")
				.AppendLine(CommandOutletColor.DarkGray, ">");
		else
			content.AppendLine(CommandOutletColor.DarkGreen, value);
	}

	private static void DumpMember(Tracker tracker, CommandOutletContent content, object value, string memberName, Type memberType, BindingFlags binding, int indent)
	{
		content.Indent(indent);

		content
			.Append(CommandOutletColor.DarkCyan, memberName)
			.Append(CommandOutletColor.DarkGray, ":")
			.Append(CommandOutletColor.DarkBlue, "(");

		if(memberType.IsEnum)
			content
				.Append(CommandOutletColor.Magenta, "enum")
				.Append(CommandOutletColor.DarkGray, ":")
				.Append(CommandOutletColor.DarkYellow, memberType.Name);
		else
			content.Append(CommandOutletColor.DarkYellow, memberType.GetAlias());

		content.Append(CommandOutletColor.DarkBlue, ")");

		var trackable = tracker.CanTrack(value);

		if(trackable)
		{
			bool tracked = false;

			try
			{
				if(tracked = tracker.Track(value))
					DumpValue(tracker, content, value, binding, indent);
				else
					content
						.Append(CommandOutletColor.DarkGray, "<")
						.Append(CommandOutletColor.DarkRed, "Circular Reference")
						.AppendLine(CommandOutletColor.DarkGray, ">");
			}
			finally
			{
				if(tracked)
					tracker.Untrack();
			}
		}
		else
			DumpValue(tracker, content, value, binding, indent);
	}

	private static CommandOutletContent Indent(this CommandOutletContent content, int indent)
	{
		if(indent > 0)
			content.Append(string.Concat(System.Linq.Enumerable.Repeat(INDENT_SYMBOL, indent)));

		return content;
	}
	#endregion

	#region 嵌套子类
	private sealed class Tracker
	{
		private readonly Stack<object> _stack = new();
		private readonly HashSet<object> _hashset = new(ReferenceComparer.Instance);

		public bool CanTrack(object value) => value != null && value is not string && value.GetType().IsClass;
		public bool Track(object value)
		{
			if(!this.CanTrack(value))
				throw new InvalidOperationException();

			if(_hashset.Add(value))
			{
				_stack.Push(value);
				return true;
			}

			return false;
		}

		public object Untrack()
		{
			if(_stack.TryPop(out var value))
			{
				_hashset.Remove(value);
				return value;
			}

			return null;
		}

		private sealed class ReferenceComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceComparer Instance = new();
			bool IEqualityComparer<object>.Equals(object x, object y) => object.ReferenceEquals(x, y);
			public int GetHashCode(object obj) => HashCode.Combine(obj);
		}
	}
	#endregion
}

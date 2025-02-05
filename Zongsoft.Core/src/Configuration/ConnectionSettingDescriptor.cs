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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public class ConnectionSettingDescriptor : IEquatable<string>, IEquatable<ConnectionSettingDescriptor>
{
	#region 常量定义
	private static readonly Type DEFAULT_TYPE = typeof(string);
	#endregion

	#region 静态字段
	/// <summary>获取分组标识描述器。</summary>
	public static readonly ConnectionSettingDescriptor Group = new(nameof(Group), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Group, Properties.Resources.ConnectionSettingDescriptor_Group_Description);
	/// <summary>获取客户端标识描述器。</summary>
	public static readonly ConnectionSettingDescriptor Client = new(nameof(Client), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Client, Properties.Resources.ConnectionSettingDescriptor_Client_Description);
	/// <summary>获取服务器地址描述器。</summary>
	public static readonly ConnectionSettingDescriptor Server = new(nameof(Server), typeof(string), true, null, Properties.Resources.ConnectionSettingDescriptor_Server, Properties.Resources.ConnectionSettingDescriptor_Server_Description);
	/// <summary>获取端口号描述器。</summary>
	public static readonly ConnectionSettingDescriptor Port = new(nameof(Port), typeof(ushort), null, Properties.Resources.ConnectionSettingDescriptor_Port, Properties.Resources.ConnectionSettingDescriptor_Port_Description);
	/// <summary>获取超时描述器。</summary>
	public static readonly ConnectionSettingDescriptor Timeout = new(nameof(Timeout), typeof(TimeSpan), null, Properties.Resources.ConnectionSettingDescriptor_Timeout, Properties.Resources.ConnectionSettingDescriptor_Timeout_Description);
	/// <summary>获取字符集描述器。</summary>
	public static readonly ConnectionSettingDescriptor Charset = new(nameof(Charset), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Charset, Properties.Resources.ConnectionSettingDescriptor_Charset_Description);
	/// <summary>获取字符编码描述器。</summary>
	public static readonly ConnectionSettingDescriptor Encoding = new(nameof(Encoding), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Encoding, Properties.Resources.ConnectionSettingDescriptor_Encoding_Description);
	/// <summary>获取提供程序描述器。</summary>
	public static readonly ConnectionSettingDescriptor Provider = new(nameof(Provider), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Provider, Properties.Resources.ConnectionSettingDescriptor_Provider_Description);
	/// <summary>获取数据库名描述器。</summary>
	public static readonly ConnectionSettingDescriptor Database = new(nameof(Database), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Database, Properties.Resources.ConnectionSettingDescriptor_Database_Description);
	/// <summary>获取连接账户描述器。</summary>
	public static readonly ConnectionSettingDescriptor UserName = new(nameof(UserName), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_UserName, Properties.Resources.ConnectionSettingDescriptor_UserName_Description);
	/// <summary>获取连接密码描述器。</summary>
	public static readonly ConnectionSettingDescriptor Password = new(nameof(Password), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Password, Properties.Resources.ConnectionSettingDescriptor_Password_Description);
	/// <summary>获取实例标识描述器。</summary>
	public static readonly ConnectionSettingDescriptor Instance = new(nameof(Instance), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Instance, Properties.Resources.ConnectionSettingDescriptor_Instance_Description);
	/// <summary>获取应用标识描述器。</summary>
	public static readonly ConnectionSettingDescriptor Application = new(nameof(Application), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Application, Properties.Resources.ConnectionSettingDescriptor_Application_Description);
	#endregion

	#region 构造函数
	public ConnectionSettingDescriptor(string name, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, false, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, false, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, object defaultValue, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, false, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, false, defaultValue, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, bool required, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, required, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, bool required, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, bool required, object defaultValue, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, required, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, bool required, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, DEFAULT_TYPE, required, defaultValue, label, description, dependencies) { }

	public ConnectionSettingDescriptor(string name, Type type, string label = null, string description = null) : this(name, null, type, false, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, Type type, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, type, false, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, Type type, object defaultValue, string label = null, string description = null) : this(name, null, type, false, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, Type type, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, type, false, defaultValue, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, Type type, bool required, string label = null, string description = null) : this(name, null, type, required, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, Type type, bool required, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, type, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, Type type, bool required, object defaultValue, string label = null, string description = null) : this(name, null, type, required, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, Type type, bool required, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, null, type, required, defaultValue, label, description, dependencies) { }

	public ConnectionSettingDescriptor(string name, string[] aliases, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, false, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, false, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, object defaultValue, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, false, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, false, defaultValue, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, bool required, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, required, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, bool required, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, bool required, object defaultValue, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, required, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, bool required, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, DEFAULT_TYPE, required, defaultValue, label, description, dependencies) { }

	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, string label = null, string description = null) : this(name, aliases, type, false, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, type, false, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, object defaultValue, string label = null, string description = null) : this(name, aliases, type, false, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, type, false, defaultValue, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, string label = null, string description = null) : this(name, aliases, type, required, null, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, type, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, object defaultValue, string label = null, string description = null) : this(name, aliases, type, required, defaultValue, label, description, null) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, object defaultValue, IEnumerable<Dependency> dependencies, string label = null, string description = null) : this(name, aliases, type, required, defaultValue, label, description, dependencies) { }

	public ConnectionSettingDescriptor(string name, Type type, bool required, string label = null, string description = null, IEnumerable<Dependency> dependencies = null) : this(name, null, type, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, Type type, bool required, object defaultValue, string label = null, string description = null, IEnumerable<Dependency> dependencies = null) : this(name, null, type, required, defaultValue, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, string label = null, string description = null, IEnumerable<Dependency> dependencies = null) : this(name, aliases, type, required, null, label, description, dependencies) { }
	public ConnectionSettingDescriptor(string name, string[] aliases, Type type, bool required, object defaultValue, string label = null, string description = null, IEnumerable<Dependency> dependencies = null)
	{
		this.Name = name ?? throw new ArgumentNullException(nameof(name));
		this.Type = type;
		this.Required = required;
		this.DefaultValue = defaultValue;
		this.Label = label;
		this.Description = description;
		this.Options = new();
		this.Dependencies = new();

		if(aliases != null && aliases.Length > 0)
		{
			aliases = aliases
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Where(alias => !string.IsNullOrEmpty(alias) && !string.Equals(this.Name, alias, StringComparison.CurrentCultureIgnoreCase))
				.ToArray();

			this.Aliases = aliases.Length == 0 ? null : aliases;
		}

		if(dependencies != null)
		{
			foreach(var dependency in dependencies)
				this.Dependencies.Add(dependency);
		}

		if(type != null && type.IsEnum)
		{
			var entries = Common.EnumUtility.GetEnumEntries(type, true);

			if(entries != null && entries.Length > 0)
			{
				for(int i = 0; i < entries.Length; i++)
					this.Options.Add(new(entries[i]));
			}
		}
	}
	#endregion

	#region 成员字段
	private object _defaultValue;
	private bool _hasDefaultValue;
	#endregion

	#region 公共属性
	/// <summary>获取连接设置项的名称。</summary>
	public string Name { get; }
	/// <summary>获取连接设置项的数据类型。</summary>
	public Type Type { get; }
	/// <summary>获取或设置连接设置项的别名。</summary>
	public string[] Aliases { get; set; }
	/// <summary>获取或设置连接设置项的格式；它通常会表示解析格式，也可以是对类型的细化说明。</summary>
	public string Format { get; set; }
	/// <summary>获取或设置一个值，指示连接设置项是否为必须设置项。</summary>
	public bool Required { get; set; }
	/// <summary>获取或设置类型转换器。</summary>
	public TypeConverter Converter { get; set; }
	/// <summary>获取或设置成员组装器。</summary>
	public TypeConverter Populator { get; set; }
	/// <summary>获取或设置连接设置项的标题。</summary>
	public string Label { get; set; }
	/// <summary>获取或设置连接设置项的描述。</summary>
	public string Description { get; set; }
	/// <summary>获取连接设置项的选项集。</summary>
	public OptionCollection Options { get; }
	/// <summary>获取连接设置项的依赖集。</summary>
	public DependencyCollection Dependencies { get; }

	/// <summary>获取或设置连接设置项的默认值。</summary>
	public object DefaultValue
	{
		get
		{
			if(_hasDefaultValue)
				return _defaultValue;

			return Common.Convert.ConvertValue(_defaultValue, this.Type, () => this.Converter, () => Common.TypeExtension.GetDefaultValue(this.Type));
		}
		set
		{
			if(_defaultValue == value)
				return;

			//确保设置的默认值是类型匹配的，否则抛出转换异常
			_defaultValue = Common.Convert.ConvertValue(value, this.Type, () => this.Converter);
			_hasDefaultValue = true;
		}
	}
	#endregion

	#region 公共方法
	public bool HasAlias(string alias) => alias != null && this.Aliases != null && this.Aliases.Length > 0 && this.Aliases.Contains(alias, StringComparer.OrdinalIgnoreCase);
	public bool HasDefaultValue(out object value)
	{
		value = _defaultValue;
		return _hasDefaultValue;
	}
	#endregion

	#region 重写方法
	public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || this.HasAlias(name);
	public bool Equals(string name, out ConnectionSettingDescriptor descriptor)
	{
		descriptor = this.Equals(name) ? this : null;
		return null != null;
	}

	public bool Equals(ConnectionSettingDescriptor other) => other is not null && this.Type == other.Type &&
	(
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) || MatchAliases(this.Aliases, other.Aliases)
	);

	public override bool Equals(object obj) => obj switch
	{
		string text => this.Equals(text),
		ConnectionSettingDescriptor descriptor => this.Equals(descriptor),
		_ => false,
	};

	public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Aliases, this.Type);
	public override string ToString() => this.DefaultValue == null ? $"[{this.Type.Name}]{this.Name}" : $"[{this.Type.Name}]{this.Name}={this.DefaultValue}";
	#endregion

	#region 私有方法
	private static bool MatchAliases(ReadOnlySpan<string> left, ReadOnlySpan<string> right)
	{
		if(left.IsEmpty)
			return right.IsEmpty;

		if(right.IsEmpty)
			return false;

		if(left.Length != right.Length)
			return false;

		for(int i = 0; i < left.Length; i++)
		{
			for(int j = 0; j < right.Length; j++)
			{
				if(string.Equals(left[i], right[j], StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}
	#endregion

	#region 嵌套子类
	[TypeConverter(typeof(OptionConverter))]
	public sealed class Option
	{
		#region 构造函数
		public Option(Common.EnumEntry entry)
		{
			this.Name = entry.Name;
			this.Value = entry.Value;
			this.Label = entry.Type == null ? null : Resources.ResourceUtility.GetResourceString(entry.Type, $"{entry.Type.Name}.{entry.Name}");
			this.Description = entry.Description;
		}

		public Option(string name, object value, string label = null, string description = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.Value = value;
			this.Label = label;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public object Value { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		#endregion

		#region 公共方法
		/// <summary>解析选项。</summary>
		/// <param name="text">指定的待解析文本，文本格式为：<c>name:value|label|description</c>，其中<c>value</c>，及其后面的<c>label</c>、<c>description</c>均可选。</param>
		/// <param name="result">输出参数，表示解析成功的结果。</param>
		/// <returns>如果解析成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		public static bool TryParse(string text, out Option result)
		{
			if(string.IsNullOrEmpty(text))
			{
				result = null;
				return false;
			}

			var parts = text.Split('|', StringSplitOptions.TrimEntries);
			var index = parts[0].IndexOfAny([':', '=']);

			result = index > 0 && index < parts[0].Length ?
				new Option(parts[0][..index], parts[0][(index + 1)..]) :
				new Option(parts[0], null);

			if(parts.Length > 1)
				result.Label = parts[1];
			if(parts.Length > 2)
				result.Description = parts[2];

			return result != null && !string.IsNullOrEmpty(result.Name);
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Value == null ? this.Name : $"{this.Name}={this.Value}";
		#endregion
	}

	public sealed class OptionCollection() : System.Collections.ObjectModel.KeyedCollection<string, Option>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(Option option) => option.Name;
	}

	[TypeConverter(typeof(DependencyConverter))]
	public sealed class Dependency
	{
		#region 构造函数
		public Dependency(string name, object value)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();
			this.Value = value;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public object Value { get; }
		#endregion

		#region 公共方法
		/// <summary>解析依赖项。</summary>
		/// <param name="text">指定的待解析文本，文本格式为：<c>name:value</c>，其中<c>value</c>可选。</param>
		/// <param name="result">输出参数，表示解析成功的结果。</param>
		/// <returns>如果解析成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		public static bool TryParse(string text, out Dependency result)
		{
			if(string.IsNullOrEmpty(text))
			{
				result = null;
				return false;
			}

			var index = text.IndexOfAny([':', '=']);

			result = index > 0 && index < text.Length ?
				new Dependency(text[..index], text[(index + 1)..]) :
				new Dependency(text, null);

			return result != null && !string.IsNullOrEmpty(result.Name);
		}
		#endregion

		#region 重写方法
		public override string ToString() => $"{this.Name}={this.Value}";
		#endregion
	}

	public sealed class DependencyCollection() : System.Collections.ObjectModel.KeyedCollection<string, Dependency>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(Dependency dependency) => dependency.Name;
	}

	private sealed class OptionConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			return value is string text && Option.TryParse(text, out var result) ? result : null;
		}
	}

	private sealed class DependencyConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			return value is string text && Dependency.TryParse(text, out var result) ? result : null;
		}
	}
	#endregion
}

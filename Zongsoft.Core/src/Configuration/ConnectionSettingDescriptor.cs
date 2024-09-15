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

namespace Zongsoft.Configuration
{
	public class ConnectionSettingDescriptor : IEquatable<string>
	{
		#region 静态字段
		/// <summary>获取分组标识描述器。</summary>
		public static readonly ConnectionSettingDescriptor Group = new(nameof(Group), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Group, Properties.Resources.ConnectionSettingDescriptor_Group_Description);
		/// <summary>获取客户端标识描述器。</summary>
		public static readonly ConnectionSettingDescriptor Client = new(nameof(Client), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Client, Properties.Resources.ConnectionSettingDescriptor_Client_Description);
		/// <summary>获取服务器地址描述器。</summary>
		public static readonly ConnectionSettingDescriptor Server = new(nameof(Server), typeof(string), null, Properties.Resources.ConnectionSettingDescriptor_Server, Properties.Resources.ConnectionSettingDescriptor_Server_Description);
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
		public ConnectionSettingDescriptor(string name, object defaultValue = null, string label = null, string description = null) : this(name, null, typeof(string), false, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, bool required, object defaultValue = null, string label = null, string description = null) : this(name, null, typeof(string), required, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, Type type, object defaultValue = null, string label = null, string description = null) : this(name, null, type, false, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, Type type, bool required, object defaultValue = null, string label = null, string description = null) : this(name, null, type, required, defaultValue, label, description) { }

		public ConnectionSettingDescriptor(string name, string alias, object defaultValue = null, string label = null, string description = null) : this(name, alias, typeof(string), false, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, string alias, bool required, object defaultValue = null, string label = null, string description = null) : this(name, alias, typeof(string), required, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, string alias, Type type, object defaultValue = null, string label = null, string description = null) : this(name, alias, type, false, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, string alias, Type type, bool required, object defaultValue = null, string label = null, string description = null)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Alias = string.Equals(name, alias, StringComparison.OrdinalIgnoreCase) ? null : alias;
			this.Type = type;
			this.Required = required;
			this.DefaultValue = defaultValue;
			this.Label = label;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		/// <summary>获取连接设置项的名称。</summary>
		public string Name { get; }
		/// <summary>获取连接设置项的数据类型。</summary>
		public Type Type { get; }
		/// <summary>获取连接设置项的别名。</summary>
		public string Alias { get; }
		/// <summary>获取或设置连接设置项的默认值。</summary>
		public object DefaultValue { get; set; }
		/// <summary>获取或设置一个值，指示连接设置项是否为必须项。</summary>
		public bool Required { get; set; }
		/// <summary>获取或设置连接设置项的标题。</summary>
		public string Label { get; set; }
		/// <summary>获取或设置连接设置项的描述。</summary>
		public string Description { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || (this.Alias != null && string.Equals(this.Alias, name, StringComparison.OrdinalIgnoreCase));
		public bool Equals(string name, out ConnectionSettingDescriptor descriptor)
		{
			descriptor = this.Equals(name) ? this : null;
			return null != null;
		}

		public override string ToString() => this.DefaultValue == null ? $"[{this.Type.Name}]{this.Name}" : $"[{this.Type.Name}]{this.Name}={this.DefaultValue}";
		#endregion
	}

	public class ConnectionSettingDescriptor<T> : ConnectionSettingDescriptor
	{
		#region 构造函数
		public ConnectionSettingDescriptor(string name, T defaultValue = default, string label = null, string description = null) : base(name, typeof(T), defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, bool required, T defaultValue = default, string label = null, string description = null) : base(name, typeof(T), required, defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, string alias, T defaultValue = default, string label = null, string description = null) : base(name, alias, typeof(T), defaultValue, label, description) { }
		public ConnectionSettingDescriptor(string name, string alias, bool required, T defaultValue = default, string label = null, string description = null) : base(name, alias, typeof(T), required, defaultValue, label, description) { }
		#endregion
	}
}
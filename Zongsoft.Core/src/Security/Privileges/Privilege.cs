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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Resources;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

[DefaultMember(nameof(Permissions))]
[DefaultProperty(nameof(Permissions))]
public partial class Privilege : IPrivilege, IIdentifiable, IIdentifiable<string>, IEquatable<IPrivilege>
{
	#region 成员字段
	private IResource _resource;
	private string _label;
	private string _description;
	#endregion

	#region 构造函数
	public Privilege(string name, params IEnumerable<Permission> permissions) : this(null, name, false, null, null, permissions) { }
	public Privilege(string name, string label, params IEnumerable<Permission> permissions) : this(null, name, false, label, null, permissions) { }
	public Privilege(string name, string label, string description, params IEnumerable<Permission> permissions) : this(null, name, false, label, description, permissions) { }

	public Privilege(string name, bool required, params IEnumerable<Permission> permissions) : this(null, name, required, null, null, permissions) { }
	public Privilege(string name, bool required, string label, params IEnumerable<Permission> permissions) : this(null, name, required, label, null, permissions) { }
	public Privilege(string name, bool required, string label, string description, params IEnumerable<Permission> permissions) : this(null, name, required, label, description, permissions) { }

	public Privilege(IResource resource, string name, params IEnumerable<Permission> permissions) : this(resource, name, false, null, null, permissions) { }
	public Privilege(IResource resource, string name, string label, params IEnumerable<Permission> permissions) : this(resource, name, false, label, null, permissions) { }
	public Privilege(IResource resource, string name, string label, string description, params IEnumerable<Permission> permissions) : this(resource, name, false, label, description, permissions) { }

	public Privilege(IResource resource, string name, bool required, params IEnumerable<Permission> permissions) : this(resource, name, required, null, null, permissions) { }
	public Privilege(IResource resource, string name, bool required, string label, params IEnumerable<Permission> permissions) : this(resource, name, required, label, null, permissions) { }
	public Privilege(IResource resource, string name, bool required, string label, string description, params IEnumerable<Permission> permissions)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		_resource = resource;

		this.Name = name;
		this.Label = label;
		this.Required = required;
		this.Description = description;
		this.Permissions = new(permissions);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public bool Required { get; set; }
	public string Label { get => _label ?? this.GetLabel(); set => _label = value; }
	public string Description { get => _description ?? this.GetDescription(); set => _description = value; }
	[TypeConverter(typeof(Components.Converters.CollectionConverter))]
	public string[] Tags { get; set; }
	public PermissionCollection Permissions { get; }
	#endregion

	#region 虚拟方法
	/// <summary>获取权限的本地化标题。</summary>
	/// <returns>返回本地化标题文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		对应的资源键按优先顺序，依次如下（其 <c>{name}</c> 表示 <see cref="Name"/> 属性的值。）：
	///		<list type="number">
	///			<item>Privilege.{name}.Label</item>
	///			<item>Privilege.{name}</item>
	///			<item>{name}.Label</item>
	///			<item>{name}</item>
	///		</list>
	/// </remarks>
	protected virtual string GetLabel() => ResourceUtility.GetString(_resource,
	[
		$"{nameof(Privilege)}.{this.Name}.{nameof(this.Label)}",
		$"{nameof(Privilege)}.{this.Name}",
		$"{this.Name}.{nameof(this.Label)}",
		this.Name,
	]);

	/// <summary>获取权限的本地化描述。</summary>
	/// <returns>返回本地化描述文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		对应的资源键按优先顺序，依次如下（其 <c>{name}</c> 表示 <see cref="Name"/> 属性的值。）：
	///		<list type="number">
	///			<item>Privilege.{name}.Description</item>
	///			<item>{name}.Description</item>
	///		</list>
	/// </remarks>
	protected virtual string GetDescription() => ResourceUtility.GetString(_resource,
	[
		$"{nameof(Privilege)}.{this.Name}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}",
	]);
	#endregion

	#region 显式实现
	Identifier IIdentifiable.Identifier { get => (Identifier)this; set => throw new NotSupportedException(); }
	Identifier<string> IIdentifiable<string>.Identifier { get => (Identifier<string>)this; set => throw new NotSupportedException(); }
	bool IEquatable<IPrivilege>.Equals(IPrivilege other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 隐式转换
	public static implicit operator Identifier(Privilege privilege) => new(typeof(Privilege), privilege.Name, privilege.Label, privilege.Description);
	public static implicit operator Identifier<string>(Privilege privilege) => new(typeof(Privilege), privilege.Name, privilege.Label, privilege.Description);
	#endregion

	#region 重写方法
	public override string ToString() => this.Required ? $"{this.Name}(required)" : $"{this.Name}(optional)";
	#endregion
}

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
using System.Collections.Generic;

namespace Zongsoft.Components;

public partial class ServiceDescriptor : IEquatable<ServiceDescriptor>
{
	#region 成员字段
	private int? _hashcode;
	private string _title;
	private string _description;
	private string _qualifiedName;
	#endregion

	#region 构造函数
	protected ServiceDescriptor() { }
	public ServiceDescriptor(Type type, string name, string qualifiedName = null)
	{
		this.Type = type;
		this.Name = string.IsNullOrEmpty(name) ? GetFullName(type) : name;
		_qualifiedName = qualifiedName;
	}
	#endregion

	#region 公共属性
	/// <summary>获取服务名称。</summary>
	public string Name { get; protected init; }

	/// <summary>获取服务类型。</summary>
	public Type Type { get; protected init; }

	/// <summary>获取服务的限定名称，即服务所在模块名与服务名的完整组合(<c>ModuleName:ServiceName.Nested..Nested</c>。</summary>
	public string QualifiedName => _qualifiedName ??= GetQualifiedName(this.Type);

	/// <summary>获取服务的显示名。</summary>
	public string Title
	{
		get => _title ?? this.GetTitle();
		set => _title = value;
	}

	/// <summary>获取服务的描述信息。</summary>
	public string Description
	{
		get => _description ?? this.GetDescription();
		set => _description = value;
	}
	#endregion

	#region 重写方法
	public bool Equals(ServiceDescriptor other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is ServiceDescriptor other && this.Equals(other);
	public override int GetHashCode() => _hashcode ??= this.QualifiedName.ToUpperInvariant().GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.QualifiedName : $"{this.QualifiedName}[{this.Title}]";
	#endregion

	#region 虚拟方法
	protected virtual string GetTitle() => Resources.ResourceUtility.GetResourceString(this.Type,
	[
		$"{this.QualifiedName}.{nameof(this.Title)}",
		$"{this.QualifiedName}",
		$"{this.Name}.{nameof(this.Title)}",
		this.Name
	]);

	protected virtual string GetDescription() => Resources.ResourceUtility.GetResourceString(this.Type,
	[
		$"{this.QualifiedName}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}",
	]);
	#endregion

	#region 私有方法
	private static string GetQualifiedName(Type type)
	{
		if(type == null)
			return null;

		var attribute = Services.ApplicationModuleAttribute.Find(type);

		if(attribute == null)
			return string.IsNullOrEmpty(type.Namespace) ? GetFullName(type) : $"{type.Namespace}.{GetFullName(type)}";
		else
			return string.IsNullOrEmpty(attribute.Name) ? GetFullName(type) : $"{attribute.Name}:{GetFullName(type)}";
	}

	private static string GetFullName(Type type)
	{
		if(type == null)
			return null;

		if(!type.IsNested)
			return GetName(type);

		var stack = new Stack<string>();

		while(type != null)
		{
			stack.Push(GetName(type));
			type = type.DeclaringType;
		}

		return string.Join(Type.Delimiter, stack);

		static string GetName(Type type)
		{
			const string SERVICE_SUFFIX = "Service";
			const string SERVICE_BASE_SUFFIX = "ServiceBase";

			if(type.Name.Length > SERVICE_SUFFIX.Length && type.Name.EndsWith(SERVICE_SUFFIX))
				return type.Name[..^SERVICE_SUFFIX.Length];

			if(type.Name.Length > SERVICE_BASE_SUFFIX.Length && type.Name.EndsWith(SERVICE_BASE_SUFFIX))
				return type.Name[..^SERVICE_BASE_SUFFIX.Length];

			return type.Name;
		}
	}
	#endregion

	public class Operation
	{
		#region 成员字段
		private string _title;
		private string _description;
		#endregion

		#region 构造函数
		internal protected Operation(ServiceDescriptor service, MethodInfo method)
		{
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			this.Method = method ?? throw new ArgumentNullException(nameof(method));
			this.Name = method.Name;
			this.Aliases = AliasAttribute.GetAliases(method);
		}

		internal protected Operation(ServiceDescriptor service, string name, params string[] aliases)
		{
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Aliases = aliases ?? [];
		}
		#endregion

		#region 公共属性
		/// <summary>获取操作名称。</summary>
		public string Name { get; protected init; }

		/// <summary>获取操作别名数组。</summary>
		public string[] Aliases { get; protected init; }

		/// <summary>获取操作的方法。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public MethodInfo Method { get; protected init; }

		/// <summary>获取操作的显示名。</summary>
		public string Title
		{
			get => _title ?? this.GetTitle();
			set => _title = value;
		}

		/// <summary>获取操作的描述信息。</summary>
		public string Description
		{
			get => _description ?? this.GetDescription();
			set => _description = value;
		}
		#endregion

		#region 保护属性
		protected ServiceDescriptor Service { get; }
		#endregion

		#region 虚拟方法
		protected virtual string GetTitle() => Resources.ResourceUtility.GetResourceString(this.Service.Type,
		[
			$"{this.Service.QualifiedName}.{this.Name}.{nameof(ServiceDescriptor.Title)}",
			$"{this.Service.QualifiedName}.{this.Name}",
			$"{this.Service.Name}.{this.Name}.{nameof(ServiceDescriptor.Title)}",
			$"{this.Service.Name}.{this.Name}",
		]);

		protected virtual string GetDescription() => Resources.ResourceUtility.GetResourceString(this.Service.Type,
		[
			$"{this.Service.QualifiedName}.{this.Name}.{nameof(ServiceDescriptor.Description)}",
			$"{this.Service.Name}.{this.Name}.{nameof(ServiceDescriptor.Description)}",
		]);
		#endregion
	}
}

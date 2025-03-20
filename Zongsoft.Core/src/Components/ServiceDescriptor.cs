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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public partial class ServiceDescriptor : IEquatable<ServiceDescriptor>
{
	#region 常量定义
	private const string SUFFIX = "Service";
	#endregion

	#region 成员字段
	private string _qualifiedName;
	private string _title;
	private string _description;
	#endregion

	#region 构造函数
	public ServiceDescriptor(Type type) : this(null, type) { }
	public ServiceDescriptor(string name, Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		if(string.IsNullOrEmpty(name))
			name = type.Name.Length > SUFFIX.Length && type.Name.EndsWith(SUFFIX) ? type.Name[..^SUFFIX.Length] : type.Name;

		this.Name = name;
		this.Type = type;
		this.Operations = new(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取服务名称。</summary>
	public string Name { get; }

	/// <summary>获取服务类型。</summary>
	public Type Type { get; }

	/// <summary>获取服务的限定名称，即服务所在模块名与服务名的完整组合(<c>ModuleName.ServiceName</c>。</summary>
	public string QualifiedName
	{
		get
		{
			if(string.IsNullOrEmpty(_qualifiedName))
			{
				var attribute = Services.ApplicationModuleAttribute.Find(this.Type);

				if(attribute == null)
					_qualifiedName = string.IsNullOrEmpty(this.Type.Namespace) ? this.Name : $"{this.Type.Namespace}.{this.Name}";
				else
					_qualifiedName = string.IsNullOrEmpty(attribute.Name) ? this.Name : $"{attribute.Name}:{this.Name}";
			}

			return _qualifiedName;
		}
	}

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

	/// <summary>获取服务操作集。</summary>
	public OperationCollection Operations { get; }
	#endregion

	#region 重写方法
	public bool Equals(ServiceDescriptor other) => string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is ServiceDescriptor other && this.Equals(other);
	public override int GetHashCode() => this.QualifiedName.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.QualifiedName : $"{this.QualifiedName}[{this.Title}]";
	#endregion

	#region 私有方法
	private string GetTitle() => Resources.ResourceUtility.GetResourceString(this.Type,
	[
		$"{this.Name}.{nameof(ServiceDescriptor.Title)}",
		this.Name
	]);

	private string GetDescription() => Resources.ResourceUtility.GetResourceString(this.Type, $"{this.Name}.{nameof(ServiceDescriptor.Description)}");
	#endregion

	public class Operation
	{
		#region 成员字段
		private string _title;
		private string _description;
		private readonly ServiceDescriptor _service;
		#endregion

		#region 构造函数
		internal Operation(ServiceDescriptor service, MethodInfo method)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			this.Method = method ?? throw new ArgumentNullException(nameof(method));
			this.Name = method.Name;
			this.Alias = method.GetCustomAttribute<AliasAttribute>()?.Alias;
		}

		internal Operation(ServiceDescriptor service, string name, string alias = null)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Alias = alias;
		}
		#endregion

		#region 公共属性
		/// <summary>获取操作名称。</summary>
		public string Name { get; }

		/// <summary>获取操作别名。</summary>
		public string Alias { get; }

		/// <summary>获取操作的方法。</summary>
		public MethodInfo Method { get; }

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

		#region 私有方法
		private string GetTitle() => Resources.ResourceUtility.GetResourceString(_service.Type,
		[
			$"{_service.Name}.{this.Name}.{nameof(ServiceDescriptor.Title)}",
			$"{_service.Name}.{this.Name}"
		]);

		private string GetDescription() => Resources.ResourceUtility.GetResourceString(_service.Type, $"{_service.Name}.{this.Name}.{nameof(ServiceDescriptor.Description)}");
		#endregion
	}

	public sealed class OperationCollection(ServiceDescriptor service) : IReadOnlyCollection<Operation>
	{
		#region 私有字段
		private readonly ServiceDescriptor _service = service;
		private readonly Dictionary<string, Operation> _operations = new();
		#endregion

		#region 公共属性
		public int Count => _operations.Count;
		public Operation this[string name] => _operations[name];
		#endregion

		#region 公共方法
		public bool TryGetValue(string name, out Operation value) => _operations.TryGetValue(name, out value);

		public Operation Add(MethodInfo method)
		{
			var operation = new Operation(_service, method);
			_operations.Add(operation.Name, operation);
			return operation;
		}

		public Operation Add(string name, string alias = null)
		{
			var operation = new Operation(_service, name, alias);
			_operations.Add(name, operation);
			return operation;
		}

		public IEnumerator<Operation> GetEnumerator() => _operations.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion
	}
}

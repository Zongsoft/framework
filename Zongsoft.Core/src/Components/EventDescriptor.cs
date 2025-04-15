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
 * Copyright (C) 2020-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Components;

[DefaultMember(nameof(Handlers))]
public class EventDescriptor : IEquatable<EventDescriptor>
{
	#region 成员字段
	private EventRegistryBase _registry;
	private string _qualifiedName;
	private string _title;
	private string _description;
	#endregion

	#region 构造函数
	public EventDescriptor(string name, string title = null, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		_qualifiedName = name;

		this.Name = name;
		this.Title = title;
		this.Description = description;
		this.Handlers = new List<IHandler>();
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件名称。</summary>
	public string Name { get; }
	/// <summary>获取事件的限定名称。</summary>
	public string QualifiedName => _qualifiedName;

	/// <summary>获取事件的显示名。</summary>
	public string Title
	{
		get => _title ?? this.GetTitle();
		set => _title = value;
	}

	/// <summary>获取事件的描述信息。</summary>
	public string Description
	{
		get => _description ?? this.GetDescription();
		set => _description = value;
	}

	/// <summary>获取事件处理程序集。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public ICollection<IHandler> Handlers { get; }
	#endregion

	#region 内部方法
	internal void SetRegistry(EventRegistryBase registry)
	{
		_registry = registry;
		_qualifiedName = registry == null ? this.Name : $"{registry.Name}:{this.Name}";
	}
	#endregion

	#region 处理方法
	public ValueTask HandleAsync(CancellationToken cancellation = default) => this.HandleAsync(null, null, cancellation);
	public ValueTask HandleAsync(Collections.Parameters parameters, CancellationToken cancellation = default) => this.HandleAsync(null, parameters, cancellation);
	public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
	public ValueTask HandleAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		var tasks = new List<Task>(this.Handlers.Count);

		foreach(var handler in this.Handlers)
		{
			tasks.Add(handler.HandleAsync(argument, parameters, cancellation).AsTask());
		}

		return new ValueTask(Task.WhenAll(tasks));
	}
	#endregion

	#region 重写方法
	public bool Equals(EventDescriptor other) => string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is EventDescriptor other && this.Equals(other);
	public override int GetHashCode() => this.QualifiedName.GetHashCode();
	public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.QualifiedName : $"{this.QualifiedName}[{this.Title}]";
	#endregion

	#region 私有方法
	private string GetTitle() =>　_registry == null ? null :
		Resources.ResourceUtility.GetResourceString(_registry.GetType(), [$"{this.Name}.{nameof(EventDescriptor.Title)}", this.Name]);

	private string GetDescription() => _registry == null ? null :
		Resources.ResourceUtility.GetResourceString(_registry.GetType(), $"{this.Name}.{nameof(EventDescriptor.Description)}");
	#endregion
}

public class EventDescriptor<TArgument> : EventDescriptor
{
	#region 构造函数
	public EventDescriptor(string name, string title = null, string description = null) : base(name, title, description) { }
	#endregion

	#region 处理方法
	public ValueTask HandleAsync(TArgument argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
	public ValueTask HandleAsync(TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		var tasks = new List<Task>(this.Handlers.Count);

		foreach(var handler in this.Handlers)
		{
			if(handler is IHandler<TArgument> generic)
				tasks.Add(generic.HandleAsync(argument, parameters, cancellation).AsTask());
			else
				tasks.Add(handler.HandleAsync(argument, parameters, cancellation).AsTask());
		}

		return new ValueTask(Task.WhenAll(tasks));
	}
	#endregion
}
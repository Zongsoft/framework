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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

using Zongsoft.Resources;
using Zongsoft.Configuration;

namespace Zongsoft.Learning;

public class TrainerDescriptor
{
	#region 构造函数
	public TrainerDescriptor(string name, string title = null, string description = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		this.Name = name;
		this.Title = title;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Title { get => field ?? this.GetTitle(); set; }
	public string Description { get => field ?? this.GetDescription(); set; }

	public ITrainerBuilder Builder { get; set; }
	public IConnectionSettingsDriver Driver
	{
		get => field ??= ConnectionSettings.Drivers.TryGetValue($"ML.{this.Name}", out var driver) ? driver : null;
		set => field = value;
	}
	#endregion

	#region 私有方法
	/// <summary>获取本地化标题。</summary>
	/// <returns>返回本地化标题文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<list type="number">
	///			<item>Trainer.{name}.Title</item>
	///			<item>{name}.Title</item>
	///			<item>Trainer.{name}</item>
	///			<item>{name}</item>
	///		</list>
	/// </remarks>
	private string GetTitle() => ResourceUtility.GetResourceString(this.GetResourceLocator(),
	[
		$"Trainer.{this.Name}.{nameof(this.Title)}",
		$"{this.Name}.{nameof(this.Title)}",
		$"Trainer.{this.Name}",
		this.Name,
	]);

	/// <summary>获取本地化描述。</summary>
	/// <returns>返回本地化描述文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<list type="number">
	///			<item>Trainer.{name}.Description</item>
	///			<item>{name}.Description</item>
	///		</list>
	/// </remarks>
	private string GetDescription() => ResourceUtility.GetResourceString(this.GetResourceLocator(),
	[
		$"Trainer.{this.Name}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}",
	]);

	private Type GetResourceLocator()
	{
		if(this.Builder != null)
			return this.Builder.GetType();
		if(this.Driver != null)
			return this.Driver.GetType();

		return this.GetType();
	}
	#endregion
}

public class TrainerDescriptorCollection() : KeyedCollection<string, TrainerDescriptor>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(TrainerDescriptor trainer) => trainer.Name;
}

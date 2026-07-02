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

using Zongsoft.Resources;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Learning;

public class EstimatorDescriptorCatalog : CategoryBase<EstimatorDescriptorCatalog>, IDiscriminator
{
	#region 内部构造
	internal EstimatorDescriptorCatalog()
	{
		this.Estimators = new();
		this.Catalogs = new(this);
	}
	#endregion

	#region 公共构造
	public EstimatorDescriptorCatalog(string name, string title = null, string description = null) : this(null, name, title, description) { }
	public EstimatorDescriptorCatalog(IResource resource, string name, string title = null, string description = null) : base(resource, name, title, description)
	{
		this.Estimators = new();
		this.Catalogs = new(this);
	}
	#endregion

	#region 公共属性
	public EstimatorDescriptorCollection Estimators { get; }
	public EstimatorDescriptorCatalogCollection Catalogs { get; }
	public EstimatorDescriptor this[string name] => this.Estimators[name];
	#endregion

	#region 重写方法
	protected override IHierarchicalNodeCollection<EstimatorDescriptorCatalog> Nodes => this.Catalogs;

	/// <summary>获取类别的本地化标题。</summary>
	/// <returns>返回本地化标题文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>标题对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
	///			<item>Estimator.{path}.Category.Title</item>
	///			<item>Estimator.{path}.Category</item>
	///			<item>Estimator.{path}.Title</item>
	///			<item>Estimator.{path}</item>
	///			<item>Estimator.{name}.Category.Title</item>
	///			<item>Estimator.{name}.Category</item>
	///			<item>Estimator.{name}.Title</item>
	///			<item>Estimator.{name}</item>
	///			<item>{path}.Category.Title</item>
	///			<item>{path}.Category</item>
	///			<item>{path}.Title</item>
	///			<item>{path}</item>
	///			<item>{name}.Category.Title</item>
	///			<item>{name}.Category</item>
	///			<item>{name}.Title</item>
	///			<item>{name}</item>
	///		</list>
	/// </remarks>
	protected override string GetTitle() => this.Resource.GetString(
	[
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Title)}",
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}",
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Title)}",
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}",
		$"Estimator.{this.Name}.{nameof(Category)}.{nameof(this.Title)}",
		$"Estimator.{this.Name}.{nameof(Category)}",
		$"Estimator.{this.Name}.{nameof(this.Title)}",
		$"Estimator.{this.Name}",
	]) ?? base.GetTitle();

	/// <summary>获取类别的本地化描述。</summary>
	/// <returns>返回本地化描述文本，如果失败则返回空(<c>null</c>)。</returns>
	/// <remarks>
	///		<para>对应的资源键按优先顺序，依次如下：</para>
	///		<para>提示：其 <c>{name}</c> 表示 <see cref="HierarchicalNode.Name"/> 属性的值；<c>{path}</c> 表示 <see cref="HierarchicalNode.FullPath"/> 属性的值。</para>
	///		<list type="number">
	///			<item>Estimator.{path}.Category.Description</item>
	///			<item>Estimator.{path}.Description</item>
	///			<item>Estimator.{name}.Category.Description</item>
	///			<item>Estimator.{name}.Description</item>
	///			<item>{path}.Category.Description</item>
	///			<item>{path}.Description</item>
	///			<item>{name}.Category.Description</item>
	///			<item>{name}.Description</item>
	///		</list>
	/// </remarks>
	protected override string GetDescription() => this.Resource.GetString(
	[
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(Category)}.{nameof(this.Description)}",
		$"Estimator.{this.FullPath.Trim(PathSeparator).Replace(PathSeparator, '.')}.{nameof(this.Description)}",
		$"Estimator.{this.Name}.{nameof(Category)}.{nameof(this.Description)}",
		$"Estimator.{this.Name}.{nameof(this.Description)}",
	]) ?? base.GetDescription();
	#endregion

	#region 显式实现
	object IDiscriminator.Discriminate(object argument)
	{
		switch(argument)
		{
			case string type:
				if(string.IsNullOrEmpty(type) || type.Equals("catalog", StringComparison.OrdinalIgnoreCase) || type.Equals(nameof(Category), StringComparison.OrdinalIgnoreCase))
					return this.Catalogs;

				if(string.Equals(type, "estimator", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "trainer", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "transformer", StringComparison.OrdinalIgnoreCase))
					return this.Estimators;

				break;
			case EstimatorDescriptor:
				return this.Estimators;
			case EstimatorDescriptorCatalog:
				return this.Catalogs;
		}

		return null;
	}
	#endregion
}

public sealed class EstimatorDescriptorCatalogCollection : CategoryCollectionBase<EstimatorDescriptorCatalog>
{
	internal EstimatorDescriptorCatalogCollection(EstimatorDescriptorCatalog owner) : base(owner) { }
}

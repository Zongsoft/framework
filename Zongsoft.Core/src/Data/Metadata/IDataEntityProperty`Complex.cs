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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据实体复合属性的元数据类。</summary>
public interface IDataEntityComplexProperty : IDataEntityProperty
{
	/// <summary>获取或设置属性的特性。</summary>
	DataEntityComplexPropertyBehaviors Behaviors { get; set; }

	/// <summary>获取关联的外部实体。</summary>
	IDataEntity Foreign { get; }

	/// <summary>获取关联的外部层级属性，只有多级关联该属性才不为空(<c>null</c>)。</summary>
	IDataEntityProperty ForeignProperty { get; }

	/// <summary>获取或设置一个值，指示关联的重复性关系。</summary>
	DataAssociationMultiplicity Multiplicity { get; set; }

	/// <summary>获取关联目标，通常它是目标实体名，也支持跳跃关联(即关联到一个复合属性)。</summary>
	/// <remarks>跳跃关联是指关联目标为实体的导航属性，实体与导航属性之间以冒号(<c>:</c>)区隔。</remarks>
	string Port { get; }

	/// <summary>获取关联的连接数组。</summary>
	DataAssociationLink[] Links { get; }

	/// <summary>获取关联的约束数组。</summary>
	DataAssociationConstraint[] Constraints { get; }
}

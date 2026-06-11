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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Upgrading.Models;

/// <summary>表示实例的实体类。</summary>
public abstract class Instance
{
	#region 公共属性
	/// <summary>获取或设置实例编号。</summary>
	public abstract uint InstanceId { get; set; }
	/// <summary>获取或设置实例代号。</summary>
	public abstract string InstanceCode { get; set; }
	/// <summary>获取或设置实例名称。</summary>
	public abstract string Name { get; set; }
	/// <summary>获取或设置标签集。</summary>
	public abstract string Tags { get; set; }
	/// <summary>获取或设置配置信息。</summary>
	public abstract string Profile { get; set; }
	/// <summary>获取或设置创建时间。</summary>
	public abstract DateTime Creation { get; set; }
	/// <summary>获取或设置修改时间。</summary>
	public abstract DateTime? Modification { get; set; }
	/// <summary>获取或设置描述说明。</summary>
	public abstract string Description { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置该实例的发布跟踪集。</summary>
	public abstract IEnumerable<ReleaseTracing> Tracings { get; set; }
	#endregion
}

/// <summary>表示实例查询条件的实体类。</summary>
public abstract class InstanceCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置实例代号。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string InstanceCode { get; set; }
	/// <summary>获取或设置实例名称。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Name { get; set; }
	/// <summary>获取或设置标签集。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Tags { get; set; }
	/// <summary>获取或设置创建时间范围。</summary>
	public abstract Range<DateTime>? Creation { get; set; }
	#endregion
}

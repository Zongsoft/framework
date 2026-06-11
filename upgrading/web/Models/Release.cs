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

/// <summary>表示发布的实体类。</summary>
public abstract class Release
{
	#region 公共属性
	/// <summary>获取或设置发布编号。</summary>
	public abstract uint ReleaseId { get; set; }
	/// <summary>获取或设置应用名称。</summary>
	public abstract string Name { get; set; }
	/// <summary>获取或设置版本名。</summary>
	public abstract string Edition { get; set; }
	/// <summary>获取或设置版本号。</summary>
	public abstract Versioning.Version.Number Version { get; set; }
	/// <summary>获取或设置发布类型。</summary>
	public abstract ReleaseKind Kind { get; set; }
	/// <summary>获取或设置升级部署模式。</summary>
	public abstract ReleaseMode Mode { get; set; }
	/// <summary>获取或设置平台。</summary>
	public abstract Platform Platform { get; set; }
	/// <summary>获取或设置架构。</summary>
	public abstract Architecture Architecture { get; set; }
	/// <summary>获取或设置文件路径。</summary>
	public abstract string Path { get; set; }
	/// <summary>获取或设置包大小。</summary>
	public abstract uint Size { get; set; }
	/// <summary>获取或设置校验码。</summary>
	public abstract string Checksum { get; set; }
	/// <summary>获取或设置标签集。</summary>
	public abstract string Tags { get; set; }
	/// <summary>获取或设置是否废弃。</summary>
	public abstract bool Deprecated { get; set; }
	/// <summary>获取或设置是否已发布。</summary>
	public abstract bool Published { get; set; }
	/// <summary>获取或设置是否可见。</summary>
	public abstract bool Visible { get; set; }
	/// <summary>获取或设置标题。</summary>
	public abstract string Title { get; set; }
	/// <summary>获取或设置摘要。</summary>
	public abstract string Summary { get; set; }
	/// <summary>获取或设置评估器名称。</summary>
	public abstract string EvaluatorName { get; set; }
	/// <summary>获取或设置评估器设置。</summary>
	public abstract string EvaluatorSetting { get; set; }
	/// <summary>获取或设置创建时间。</summary>
	public abstract DateTime Creation { get; set; }
	/// <summary>获取或设置修改时间。</summary>
	public abstract DateTime? Modification { get; set; }
	/// <summary>获取或设置描述信息。</summary>
	public abstract string Description { get; set; }
	#endregion

	#region 集合属性
	/// <summary>获取或设置发布属性集。</summary>
	public abstract ICollection<ReleaseProperty> Properties { get; set; }
	/// <summary>获取或设置发布执行器集。</summary>
	public abstract ICollection<ReleaseExecutor> Executors { get; set; }
	/// <summary>获取或设置发布跟踪集。</summary>
	public abstract IEnumerable<ReleaseTracing> Tracings { get; set; }
	#endregion
}

/// <summary>表示发布查询条件的实体类。</summary>
public abstract class ReleaseCriteria : CriteriaBase
{
	#region 公共属性
	/// <summary>获取或设置应用名称。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Name { get; set; }
	/// <summary>获取或设置版本名。</summary>
	public abstract string Edition { get; set; }
	/// <summary>获取或设置版本号。</summary>
	public abstract Range<Versioning.Version.Number> Version { get; set; }
	/// <summary>获取或设置发布类型。</summary>
	public abstract ReleaseKind? Kind { get; set; }
	/// <summary>获取或设置升级部署模式。</summary>
	public abstract ReleaseMode? Mode { get; set; }
	/// <summary>获取或设置平台。</summary>
	public abstract Platform? Platform { get; set; }
	/// <summary>获取或设置架构。</summary>
	public abstract Architecture? Architecture { get; set; }
	/// <summary>获取或设置标签集。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Tags { get; set; }
	/// <summary>获取或设置是否废弃。</summary>
	public abstract bool? Deprecated { get; set; }
	/// <summary>获取或设置是否已发布。</summary>
	public abstract bool? Published { get; set; }
	/// <summary>获取或设置是否可见。</summary>
	public abstract bool? Visible { get; set; }
	/// <summary>获取或设置标题。</summary>
	[Condition(ConditionOperator.Like)]
	public abstract string Title { get; set; }
	/// <summary>获取或设置评估器名称。</summary>
	public abstract string EvaluatorName { get; set; }
	/// <summary>获取或设置创建时间范围。</summary>
	public abstract Range<DateTime>? Creation { get; set; }
	#endregion
}

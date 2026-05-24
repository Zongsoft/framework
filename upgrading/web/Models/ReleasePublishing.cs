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

namespace Zongsoft.Upgrading.Models;

/// <summary>表示发布实例状态的实体类。</summary>
public abstract class ReleasePublishing
{
	#region 公共属性
	/// <summary>获取或设置发布编号。</summary>
	public abstract uint ReleaseId { get; set; }
	/// <summary>获取或设置实例编号。</summary>
	public abstract uint InstanceId { get; set; }
	/// <summary>获取或设置发布状态。</summary>
	public abstract ReleasePublishingStatus Status { get; set; }
	/// <summary>获取或设置失败消息。</summary>
	public abstract string Message { get; set; }
	/// <summary>获取或设置更新时间。</summary>
	public abstract DateTime Timestamp { get; set; }
	/// <summary>获取或设置更新描述。</summary>
	public abstract string Description { get; set; }
	/// <summary>获取或设置所属发布。</summary>
	public abstract Release Release { get; set; }
	/// <summary>获取或设置所属实例。</summary>
	public abstract Instance Instance { get; set; }
	#endregion
}

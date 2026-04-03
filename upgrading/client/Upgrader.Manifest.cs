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
using System.Linq;

namespace Zongsoft.Upgrading;

partial class Upgrader
{
	/// <summary>表示升级发布的清单类。</summary>
	public sealed class Manifest
	{
		#region 常量定义
		/// <summary>清单文件名。</summary>
		public const string FileName = ".manifest";
		#endregion

		#region 构造函数
		public Manifest() => this.Deltas = [];
		public Manifest(Release[] deltas) => this.Deltas = deltas ?? [];
		public Manifest(Release baseline, Release[] deltas)
		{
			this.Baseline = baseline;
			this.Deltas = deltas ?? [];
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置升级的基线全量升级包。</summary>
		public Release Baseline { get; set; }
		/// <summary>获取或设置升级的增量升级包集合。</summary>
		public Release[] Deltas { get; set; }

		/// <summary>获取发布清单名称。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public string Name => this.Baseline != null ? this.Baseline.Name : this.Deltas.Length > 0 ? this.Deltas[0].Name : null;

		/// <summary>获取发布清单版本号，即本次升级发布中的最高版本。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public Version Version => this.Deltas.Max(delta => delta.Version) ?? this.Baseline?.Version;

		/// <summary>获取一个值，指示当前升级清单是否为空。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public bool IsEmpty => this.Baseline == null && (this.Deltas == null || this.Deltas.Length == 0);
		#endregion

		#region 重写方法
		public override string ToString() => this.IsEmpty ? string.Empty : $"{this.Name}@{this.Version}";
		#endregion
	}
}

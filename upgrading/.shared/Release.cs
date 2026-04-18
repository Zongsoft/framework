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

namespace Zongsoft.Upgrading;

/// <summary>表示升级发布的实体类。</summary>
public partial class Release
{
	#region 构造函数
	public Release()
	{
		this.Tags = [];
		this.Executors = [];
		this.Creation = DateTime.Now;
		this.Platform = Application.Platform;
		this.Architecture = Application.Architecture;
		this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public Release(string name, Version version, string[] tags = null) : this(name, null, version, tags) { }
	public Release(string name, string edition, Version version, string[] tags = null)
	{
		this.Name = name;
		this.Edition = edition;
		this.Version = version;
		this.Tags = tags ?? [];
		this.Executors = [];
		this.Creation = DateTime.Now;
		this.Platform = Application.Platform;
		this.Architecture= Application.Architecture;
		this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}

	public Release(string name, Version version, Platform platform, Architecture architecture, string[] tags = null) : this(name, null, version, platform, architecture, tags) { }
	public Release(string name, string edition, Version version, Platform platform, Architecture architecture, string[] tags = null)
	{
		this.Name = name;
		this.Edition = edition;
		this.Version = version;
		this.Tags = tags ?? [];
		this.Executors = [];
		this.Creation = DateTime.Now;
		this.Platform = platform;
		this.Architecture = architecture;
		this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置发布名称（应用名称）。</summary>
	public string Name { get; set; }
	/// <summary>获取或设置发布标题。</summary>
	public string Title { get; set; }
	/// <summary>获取或设置发布概述。</summary>
	public string Summary { get; set; }
	/// <summary>获取或设置发布版本。</summary>
	public string Edition { get; set; }
	/// <summary>获取或设置发布种类。</summary>
	public ReleaseKind Kind { get; set; }
	/// <summary>获取或设置发布文件的路径。</summary>
	public string Path { get; set; }
	/// <summary>获取或设置发布文件的大小（单位：字节）。</summary>
	public uint Size { get; set; }
	/// <summary>获取或设置发布文件的校验码。</summary>
	public Common.Checksum Checksum { get; set; }
	/// <summary>获取或设置发布标签集。</summary>
	public string[] Tags { get; set; }
	/// <summary>获取或设置发布版本号。</summary>
	public Version Version { get; set; }
	/// <summary>获取或设置系统平台。</summary>
	public Platform Platform { get; set; }
	/// <summary>获取或设置体系架构。</summary>
	public Architecture Architecture { get; set; }
	/// <summary>获取或设置一个值，指示本发布是否已弃用。</summary>
	public bool Deprecated { get; set; }
	/// <summary>获取或设置发布时间。</summary>
	public DateTime Creation { get; set; }
	/// <summary>获取或设置描述信息。</summary>
	public string Description { get; set; }

	/// <summary>获取执行器集合。</summary>
	public ICollection<Executor> Executors { get; }

	/// <summary>获取发布扩展属性集。</summary>
	public IDictionary<string, object> Properties { get; }
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Edition) ?
		$"{this.Name}@{this.Version}({this.GetRuntimeIdentifier()})":
		$"{this.Name}|{this.Edition}@{this.Version}({this.GetRuntimeIdentifier()})";
	#endregion

	#region 嵌套结构
	public struct Executor(string @event, string command)
	{
		#region 公共字段
		public string Event = @event;
		public string Command = command;
		#endregion

		#region 重写方法
		public readonly override string ToString() => $"[{this.Event}]{this.Command}";
		#endregion
	}
	#endregion
}

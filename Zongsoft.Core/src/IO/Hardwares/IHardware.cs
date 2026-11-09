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

namespace Zongsoft.IO.Hardwares;

/// <summary>表示硬件设备。</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(Hardware.JsonConverter))]
public interface IHardware
{
	#region 公共属性
	/// <summary>获取标识代码。</summary>
	string Code { get; }

	/// <summary>获取标识名称。</summary>
	string Name { get; }

	/// <summary>获取标识类型。</summary>
	string Type { get; }

	/// <summary>获取硬件型号。</summary>
	string Model { get; }

	/// <summary>获取硬件系列。</summary>
	string Serie { get; }

	/// <summary>获取硬件分类，多级分类以斜杠(<c>/</c>)分隔。</summary>
	string Category { get; }

	/// <summary>获取标识描述。</summary>
	string Description { get; }

	/// <summary>获取硬件制造商。</summary>
	string Manufacturer { get; }

	/// <summary>获取硬件驱动程序。</summary>
	IHardwareDriver Driver { get; }

	/// <summary>获取硬件组件集。</summary>
	HardwareComponentCollection Components { get; }

	/// <summary>获取硬件属性集。</summary>
	HardwarePropertyCollection Properties { get; }
	#endregion

	#region 公共方法
	/// <summary>获取一个值，指示当前对象是否具有唯一编号。</summary>
	/// <param name="identifier">返回当前对象的唯一编号。</param>
	/// <returns>如果当前对象具有唯一编号则返回真(<c>true</c>)，否则返回假(<c>false</c>)。</returns>
	bool HasUnique(out string identifier);
	#endregion
}

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

/// <summary>表示硬件驱动程序。</summary>
public interface IHardwareDriver
{
	/// <summary>获取驱动程序名称。</summary>
	string Name { get; }

	/// <summary>获取驱动程序标题。</summary>
	string Caption { get; }

	/// <summary>获取驱动程序版本。</summary>
	string Version { get; }

	/// <summary>获取驱动程序制造商。</summary>
	string Manufacturer { get; }

	/// <summary>获取驱动程序描述。</summary>
	string Description { get; }
}

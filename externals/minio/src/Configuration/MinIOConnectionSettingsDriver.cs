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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.MinIO library.
 *
 * The Zongsoft.Externals.MinIO is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.MinIO is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.MinIO library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.MinIO.Configuration;

public class MinIOConnectionSettingsDriver : ConnectionSettingsDriver<MinIOConnectionSettings>
{
	#region 常量定义
	internal const string NAME = "MinIO";
	#endregion

	#region 单例字段
	public static readonly MinIOConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private MinIOConnectionSettingsDriver() : base(NAME) { }
	#endregion
}

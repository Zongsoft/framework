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

using Zongsoft.Configuration;

namespace Zongsoft.Learning;

public class ConcatenatingEstimatorSettings : ConnectionSettingsBase<ConcatenatingEstimatorSettingsDriver>
{
	#region 构造函数
	public ConcatenatingEstimatorSettings(ConcatenatingEstimatorSettingsDriver driver, string settings) : base(driver, settings) { }
	public ConcatenatingEstimatorSettings(ConcatenatingEstimatorSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public string Output
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string[] Inputs
	{
		get => this.GetValue<string[]>();
		set => this.SetValue(value);
	}
	#endregion
}

public class ConcatenatingEstimatorSettingsDriver : ConnectionSettingsDriver<ConcatenatingEstimatorSettings>
{
	#region 常量定义
	internal const string NAME = "ML.Concatenating";
	#endregion

	#region 单例字段
	public static readonly ConcatenatingEstimatorSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private ConcatenatingEstimatorSettingsDriver() : base(NAME) { }
	#endregion
}
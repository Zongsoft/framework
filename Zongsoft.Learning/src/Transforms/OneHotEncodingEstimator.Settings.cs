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
using System.ComponentModel;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Learning.Transforms;

public class OneHotEncodingEstimatorSettings : ConnectionSettingsBase<OneHotEncodingEstimatorSettingsDriver>
{
	#region 构造函数
	public OneHotEncodingEstimatorSettings(OneHotEncodingEstimatorSettingsDriver driver, string settings) : base(driver, settings) { }
	public OneHotEncodingEstimatorSettings(OneHotEncodingEstimatorSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[DefaultValue(Microsoft.ML.Transforms.OneHotEncodingEstimator.OutputKind.Indicator)]
	public Microsoft.ML.Transforms.OneHotEncodingEstimator.OutputKind Kind
	{
		get => this.GetValue<Microsoft.ML.Transforms.OneHotEncodingEstimator.OutputKind>();
		set => this.SetValue(value);
	}

	[Alias("KeyOrdinality")]
	[DefaultValue(Microsoft.ML.Transforms.ValueToKeyMappingEstimator.KeyOrdinality.ByOccurrence)]
	public Microsoft.ML.Transforms.ValueToKeyMappingEstimator.KeyOrdinality Ordinality
	{
		get => this.GetValue<Microsoft.ML.Transforms.ValueToKeyMappingEstimator.KeyOrdinality>();
		set => this.SetValue(value);
	}

	[DefaultValue(1000000)]
	[Alias("MaximumNumberOfKeys")]
	public int MaximumKeys
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(typeof(InputOutputColumnPairConverter))]
	public Microsoft.ML.InputOutputColumnPair[] Columns
	{
		get => this.GetValue<Microsoft.ML.InputOutputColumnPair[]>();
		set => this.SetValue(value);
	}
	#endregion
}

public class OneHotEncodingEstimatorSettingsDriver : ConnectionSettingsDriver<OneHotEncodingEstimatorSettings>
{
	#region 常量定义
	internal const string NAME = "ML.OneHotEncoding";
	#endregion

	#region 单例字段
	public static readonly OneHotEncodingEstimatorSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private OneHotEncodingEstimatorSettingsDriver() : base(NAME) { }
	#endregion
}
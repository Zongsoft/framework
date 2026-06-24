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

using Zongsoft.Configuration;

namespace Zongsoft.Learning.Trainers;

public class LightGbmRegressionTrainerSettings : ConnectionSettingsBase<LightGbmRegressionTrainerSettingsDriver, Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options>
{
	#region 构造函数
	public LightGbmRegressionTrainerSettings(LightGbmRegressionTrainerSettingsDriver driver, string settings) : base(driver, settings) { }
	public LightGbmRegressionTrainerSettings(LightGbmRegressionTrainerSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[DefaultValue("Features")]
	public string FeatureColumnName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("Label")]
	public string LabelColumnName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string ExampleWeightColumnName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string RowGroupColumnName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(100)]
	public int NumberOfIterations
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(255)]
	public int MaximumBinCountPerFeature
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	public int? NumberOfLeaves
	{
		get => this.GetValue<int?>();
		set => this.SetValue(value);
	}

	public int? MinimumExampleCountPerLeaf
	{
		get => this.GetValue<int?>();
		set => this.SetValue(value);
	}

	public double? LearningRate
	{
		get => this.GetValue<double?>();
		set => this.SetValue(value);
	}

	public int? NumberOfThreads
	{
		get => this.GetValue<int?>();
		set => this.SetValue(value);
	}

	public int EarlyStoppingRound
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(1 << 20)]
	public int BatchSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(100)]
	public int MinimumExampleCountPerGroup
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(32)]
	public int MaximumCategoricalSplitPointCount
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(10)]
	public double CategoricalSmoothing
	{
		get => this.GetValue<double>();
		set => this.SetValue(value);
	}

	[DefaultValue(10)]
	public double L2CategoricalRegularization
	{
		get => this.GetValue<double>();
		set => this.SetValue(value);
	}

	public int? Seed
	{
		get => this.GetValue<int?>();
		set => this.SetValue(value);
	}

	public bool Deterministic
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool ForceColumnWise
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool ForceRowWise
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool? UseCategoricalSplit
	{
		get => this.GetValue<bool?>();
		set => this.SetValue(value);
	}

	[DefaultValue(true)]
	public bool HandleMissingValue
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue(false)]
	public bool UseZeroAsMissingValue
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public bool Verbose
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue(true)]
	public bool Silent
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	public BoosterOptions Booster
	{
		get => this.GetValue<BoosterOptions>();
		set => this.SetValue(value);
	}

	[DefaultValue(Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options.EvaluateMetricType.RootMeanSquaredError)]
	public Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options.EvaluateMetricType EvaluateMetricType
	{
		get => this.GetValue<Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options.EvaluateMetricType>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override void Populate(Microsoft.ML.Trainers.LightGbm.LightGbmRegressionTrainer.Options options)
	{
		options.Booster = this.Booster?.GetOptions();
	}
	#endregion

	#region 嵌套子类
	public class BoosterOptions
	{
		public BoosterOptions()
		{
			this.MinimumChildWeight = 0.1;
			this.SubsampleFraction = 1.0;
			this.FeatureFraction = 1.0;
			this.L2Regularization = 0.01;
		}

		public string Name { get; set; }
		public int MaximumTreeDepth { get; set; }
		public double MinimumSplitGain { get; set; }
		public double MinimumChildWeight { get; set; }
		public int SubsampleFrequency { get; set; }
		public double SubsampleFraction { get; set; }
		public double FeatureFraction { get; set; }
		public double L1Regularization { get; set; }
		public double L2Regularization { get; set; }

		public Microsoft.ML.Trainers.LightGbm.BoosterParameterBase.OptionsBase GetOptions()
		{
			Microsoft.ML.Trainers.LightGbm.BoosterParameterBase.OptionsBase options = this.Name switch
			{
				"dart" => new Microsoft.ML.Trainers.LightGbm.DartBooster.Options(),
				"goss" => new Microsoft.ML.Trainers.LightGbm.GossBooster.Options(),
				"gbdt" => new Microsoft.ML.Trainers.LightGbm.GradientBooster.Options(),
				_ => null,
			};

			if(options != null)
			{
				options.MaximumTreeDepth = this.MaximumTreeDepth;
				options.MinimumSplitGain = this.MinimumSplitGain;
				options.MinimumChildWeight = this.MinimumChildWeight;
				options.SubsampleFrequency = this.SubsampleFrequency;
				options.SubsampleFraction = this.SubsampleFraction;
				options.FeatureFraction = this.FeatureFraction;
				options.L1Regularization = this.L1Regularization;
				options.L2Regularization = this.L2Regularization;
			}

			return options;
		}
	}
	#endregion
}

public class LightGbmRegressionTrainerSettingsDriver : ConnectionSettingsDriver<LightGbmRegressionTrainerSettings>
{
	#region 常量定义
	internal const string NAME = "ML.LightGbm";
	#endregion

	#region 单例字段
	public static readonly LightGbmRegressionTrainerSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private LightGbmRegressionTrainerSettingsDriver() : base(NAME) { }
	#endregion
}
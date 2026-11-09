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

using System;
using System.ComponentModel;

using OpenTelemetry.Exporter;

using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Configuration;

partial class ExporterLauncherSettings
{
	public sealed class Console : ConnectionSettingsBase<IConnectionSettingsDriver<Console>>
	{
		#region 构造函数
		public Console(IConnectionSettingsDriver<Console> driver, string settings) : base(driver, settings) { }
		public Console(IConnectionSettingsDriver<Console> driver, string name, string settings) : base(driver, name, settings) { }
		#endregion

		#region 公共属性
		[DefaultValue(ConsoleExporterOutputTargets.Console)]
		public ConsoleExporterOutputTargets Target
		{
			get => this.GetValue<ConsoleExporterOutputTargets>();
			set => this.SetValue(value);
		}

		[DefaultValue("30s")]
		public TimeSpan Timeout
		{
			get => this.GetValue<TimeSpan>();
			set => this.SetValue(value);
		}

		[DefaultValue("10s")]
		public TimeSpan Interval
		{
			get => this.GetValue<TimeSpan>();
			set => this.SetValue(value);
		}
		#endregion
	}
}

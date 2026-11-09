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

using OpenTelemetry;
using OpenTelemetry.Exporter;

using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Configuration;

public sealed partial class ExporterLauncherSettings : ConnectionSettingsBase<IConnectionSettingsDriver<ExporterLauncherSettings>>
{
	#region 构造函数
	public ExporterLauncherSettings(IConnectionSettingsDriver<ExporterLauncherSettings> driver, string settings) : base(driver, settings) { }
	public ExporterLauncherSettings(IConnectionSettingsDriver<ExporterLauncherSettings> driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(OtlpExportProtocol.Grpc)]
	public OtlpExportProtocol Protocol
	{
		get => this.GetValue<OtlpExportProtocol>();
		set => this.SetValue(value);
	}

	[DefaultValue(ExportProcessorType.Batch)]
	public ExportProcessorType ProcessorType
	{
		get => this.GetValue<ExportProcessorType>();
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

	[DefaultValue(512)]
	public int MaxBatchSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(2048)]
	public int MaxQueueSize
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}
	#endregion
}

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

using Zongsoft.Collections;

namespace Zongsoft.Data.Archiving;

public class DataArchiveExtractorOptions : IDataArchiveExtractorOptions
{
	#region 成员字段
	private IDataArchivePopulator _populator;
	#endregion

	#region 构造函数
	public DataArchiveExtractorOptions(ModelDescriptor model, Parameters parameters = null) : this(model, parameters, null) { }
	public DataArchiveExtractorOptions(ModelDescriptor model, Parameters parameters, object source, params string[] members)
	{
		this.Model = model;
		this.Source = source;
		this.Members = members;
		this.Parameters = parameters ?? new();
		this.Populator = DataArchivePopulator.Default;
	}
	#endregion

	#region 公共属性
	public ModelDescriptor Model { get; }
	public object Source { get; set; }
	public string[] Members { get; set; }
	public Parameters Parameters { get; }
	public IDataArchivePopulator Populator
	{
		get => _populator;
		set => _populator = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion
}
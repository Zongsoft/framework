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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Collections;

namespace Zongsoft.Data.Templates;

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
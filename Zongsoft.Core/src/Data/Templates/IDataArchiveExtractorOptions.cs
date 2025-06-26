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

namespace Zongsoft.Data.Templates;

/// <summary>
/// 表示数据文件提取选项的接口。
/// </summary>
public interface IDataArchiveExtractorOptions
{
	/// <summary>获取模型元信息。</summary>
	ModelDescriptor Model {  get; }

	/// <summary>获取或设置提取来源。</summary>
	object Source { get; set; }

	/// <summary>获取或设置提取的成员集。</summary>
	string[] Members { get; set; }

	/// <summary>获取上下文相关参数集合。</summary>
	Collections.Parameters Parameters { get; }

	/// <summary>获取或设置数据组装器。</summary>
	IDataArchivePopulator Populator { get; set; }
}
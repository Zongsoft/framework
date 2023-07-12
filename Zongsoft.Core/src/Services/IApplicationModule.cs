/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.ComponentModel;

namespace Zongsoft.Services
{
	/// <summary>
	/// 表示应用模块（应用子系统）的接口。
	/// </summary>
	public interface IApplicationModule
	{
		/// <summary>获取应用模块名称。</summary>
		string Name { get; }

		/// <summary>获取应用模块的标题。</summary>
		string Title { get; }

		/// <summary>获取应用模块的描述文本。</summary>
		string Description { get; }

		/// <summary>获取应用模块的服务容器。</summary>
		IServiceProvider Services { get; }

		/// <summary>获取应用模块的授权目标集。</summary>
		INamedCollection<Schema> Schemas { get; }

		/// <summary>获取应用模块的自定义属性集。</summary>
		IDictionary<string, object> Properties { get; }
	}
}

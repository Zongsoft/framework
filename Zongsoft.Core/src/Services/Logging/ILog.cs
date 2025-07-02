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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Services.Logging;

/// <summary>
/// 表示日志实体的接口。
/// </summary>
public interface ILog
{
	/// <summary>获取或设置服务URL地址。</summary>
	string Url { get; set; }
	/// <summary>获取或设置服务方法。</summary>
	string Method { get; set; }
	/// <summary>获取或设置模块名称。</summary>
	string Module { get; set; }
	/// <summary>获取或设置服务标识。</summary>
	string Target { get; set; }
	/// <summary>获取或设置行为标识。</summary>
	string Action { get; set; }
	/// <summary>获取或设置日志概述。</summary>
	string Summary { get; set; }
	/// <summary>获取或设置场景标识。</summary>
	string Scenario { get; set; }
	/// <summary>获取或设置日志时间。</summary>
	DateTime Timestamp { get; set; }
	/// <summary>获取或设置描述说明。</summary>
	string Description { get; set; }
	/// <summary>获取或设置用户主体。</summary>
	System.Security.Principal.IPrincipal User { get; set; }
	/// <summary>获取扩展属性集。</summary>
	IDictionary<string, object> Properties { get; }
}

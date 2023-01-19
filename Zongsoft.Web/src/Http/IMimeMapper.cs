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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Web.Http
{
	/// <summary>
	/// 提供MIME类型映射功能的接口。
	/// </summary>
	public interface IMimeMapper : Microsoft.AspNetCore.StaticFiles.IContentTypeProvider
	{
		/// <summary>获取MIME类型的映射表。</summary>
		IDictionary<string, string> Mappings { get; }

		/// <summary>获取指定名称映射的MIME类型。</summary>
		/// <param name="path">指定的文件路径或扩展名。</param>
		/// <returns>如果映射成功则返回指定名称对应的MIME类型，否则返回空(null)。</returns>
		string GetMimeType(string path);
	}
}

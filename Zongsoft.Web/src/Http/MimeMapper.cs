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
	/// 提供MIME类型映射的实现类。
	/// </summary>
	public class MimeMapper : Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider, IMimeMapper
	{
		#region 单例字段
		public static readonly MimeMapper Default = new MimeMapper();
		#endregion

		#region 构造函数
		public MimeMapper() => Map(this.Mappings);
		public MimeMapper(IDictionary<string, string> mapping) : base(mapping) => Map(this.Mappings);
		#endregion

		#region 公共方法
		/// <inheritdoc cref="IMimeMapper.GetMimeType(string)" />
		public string GetMimeType(string path) => this.TryGetContentType(path, out var type) ? type : null;
		#endregion

		#region 私有方法
		private static void Map(IDictionary<string, string> mapping)
		{
			mapping.TryAdd(".rdlx", "application/vnd.ms-reporting+xml");
		}
		#endregion
	}
}

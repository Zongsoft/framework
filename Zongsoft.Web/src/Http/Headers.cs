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

namespace Zongsoft.Web.Http;

public static class Headers
{
	/// <summary>表示JSON序列化行为的请求头。</summary>
	public const string JsonBehaviors = "X-Json-Behaviors";

	/// <summary>表示数据模式的请求头。</summary>
	public const string DataSchema = "X-Data-Schema";

	/// <summary>表示数据导出的请求头。</summary>
	public const string ExportFields = "X-Export-Fields";

	/// <summary>表示分页信息的响应头。</summary>
	public const string Pagination = "X-Pagination";

	/// <summary>表示人机识别的请求头或响应头。</summary>
	public const string Captcha = "X-Captcha";
}

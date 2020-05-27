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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Aliyun.Storages
{
	internal class StorageHeaders
	{
		public const string OSS_PREFIX = "x-oss-";
		public const string OSS_META = OSS_PREFIX + "meta-";
		public const string OSS_COPY_SOURCE = OSS_PREFIX + "copy-source";
		public const string OSS_COPY_DIRECTIVE = OSS_PREFIX + "metadata-directive";

		//自定义扩展属性常量
		public const string ZFS_CREATEDTIME_PROPERTY = "CreatedTime";

		//标准的HTTP头的常量
		public const string HTTP_ETAG_PROPERTY = "HTTP:ETag";
		public const string HTTP_CONTENT_LENGTH_PROPERTY = "HTTP:Content-Length";
		public const string HTTP_LAST_MODIFIED_PROPERTY = "HTTP:Last-Modified";
	}
}

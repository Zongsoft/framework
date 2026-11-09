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

namespace Zongsoft.Externals.Aliyun.Storages;

internal class StorageHeaders
{
	public const string OSS_PREFIX = "x-oss-";
	public const string OSS_META = OSS_PREFIX + "meta-";
	public const string OSS_COPY_SOURCE = OSS_PREFIX + "copy-source";
	public const string OSS_COPY_DIRECTIVE = OSS_PREFIX + "metadata-directive";

	//自定义扩展属性常量
	public const string ZFS_CREATION_PROPERTY = "Creation";

	//标准的HTTP头的常量
	public const string HTTP_ETAG_PROPERTY = "HTTP:ETag";
	public const string HTTP_CONTENT_LENGTH_PROPERTY = "HTTP:Content-Length";
	public const string HTTP_LAST_MODIFIED_PROPERTY = "HTTP:Last-Modified";
}

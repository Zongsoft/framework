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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

/// <summary>表示数据源的接口。</summary>
public interface IDataSource : IEquatable<IDataSource>
{
	/// <summary>获取数据源的名称。</summary>
	string Name { get; }

	/// <summary>获取数据源的连接字符串。</summary>
	string ConnectionString { get; }

	/// <summary>获取数据源支持的访问方式。</summary>
	DataAccessMode Mode { get; set; }

	/// <summary>获取数据源关联的数据驱动器。</summary>
	IDataDriver Driver { get; }

	/// <summary>获取支持的功能特性集。</summary>
	FeatureCollection Features { get; }

	/// <summary>获取扩展属性集。</summary>
	IDictionary<string, object> Properties { get; }

	/// <summary>异步获取数据源的模式信息。</summary>
	/// <param name="name">指定要获取的模式名。</param>
	/// <param name="refresh">指定是否要刷新模式缓存。</param>
	/// <param name="cancellation">指定异步读取及等待缓存的取消标记。</param>
	/// <returns>返回获取成功的模式信息，如果为空(<c>null</c>)则表示获取失败或指定的模式不存在。</returns>
	ValueTask<DataTable> GetSchemaAsync(string name, bool refresh, CancellationToken cancellation = default);

	/// <summary>异步获取数据源的缓存模式信息。</summary>
	/// <param name="name">指定要获取的模式名。</param>
	/// <param name="cancellation">指定异步读取及等待缓存的取消标记。</param>
	/// <returns>返回模式信息，如果模式不存在或获取失败则返回空。</returns>
	ValueTask<DataTable> GetSchemaAsync(string name, CancellationToken cancellation = default) => this.GetSchemaAsync(name, false, cancellation);
}

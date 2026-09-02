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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Storages.Data library.
 *
 * The Zongsoft.Messaging.Storages.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Storages.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Storages.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Zongsoft.Data;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Storages;

/// <summary>提供基于<see cref="IDataAccess"/>的关系型数据库消息存储。</summary>
public sealed partial class DataMessageStorage : MessageStorageBase<IConnectionSettings>
{
	#region 成员字段
	private readonly string _partition;
	private readonly IDataAccess _accessor;
	private readonly CommandSet _commands;
	#endregion

	#region 构造函数
	/// <summary>初始化数据库消息存储。</summary>
	/// <param name="name">指定的存储名称。</param>
	/// <param name="accessor">指定执行数据命令的数据访问器。</param>
	/// <param name="settings">指定数据访问器使用的连接设置。</param>
	/// <param name="partition">指定当前存储器使用的数据分区。</param>
	internal DataMessageStorage(string name, IDataAccess accessor, IConnectionSettings settings, string partition) : base(name, settings ?? throw new ArgumentNullException(nameof(settings)))
	{
		_accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
		_commands = CommandSet.Resolve(settings.Driver?.Name);
		_partition = string.IsNullOrWhiteSpace(partition) ? throw new ArgumentNullException(nameof(partition)) : partition.Trim();
	}
	#endregion

	#region 内部属性
	internal string Partition => _partition;
	internal IDataAccess Accessor => _accessor;
	internal IConnectionSettings ConnectionSettings => this.Settings;
	#endregion

	#region 重写方法
	protected override ValueTask<int> OnClearAsync(string topic, CancellationToken cancellation)
	{
		if(topic == null)
			return _accessor.ExecuteAsync(_commands.Clear, [new Parameter("Namespace", _partition)], cancellation);

		return _accessor.ExecuteAsync(_commands.ClearByTopic, [
			new Parameter("Namespace", _partition),
			new Parameter("Topic", topic)], cancellation);
	}

	protected override async ValueTask OnSetAsync(Message message, TimeSpan expiry, CancellationToken cancellation)
	{
		var expiration = expiry > TimeSpan.Zero ? DateTimeOffset.UtcNow.Add(expiry) : (DateTimeOffset?)null;
		var timestamp = new DateTimeOffset(Normalize(message.Timestamp));
		var identity = message.Identity;
		var tags = message.Tags;
		var data = message.Data;

		await _accessor.ExecuteAsync(_commands.Set, [
			new Parameter("Namespace", _partition),
			new Parameter("Identifier", message.Identifier),
			new Parameter("Topic", message.Topic ?? string.Empty),
			new Parameter("Identity", identity ?? string.Empty),
			new Parameter("IdentityIsNull", identity == null),
			new Parameter("Tags", tags ?? string.Empty),
			new Parameter("TagsIsNull", tags == null),
			new Parameter("Timestamp", timestamp),
			new Parameter("Expiration", expiration ?? timestamp),
			new Parameter("ExpirationIsNull", !expiration.HasValue),
			new Parameter("Data", data == null ? [] : (byte[])data.Clone()),
			new Parameter("DataIsNull", data == null)], cancellation);
	}

	protected override async ValueTask<bool> OnRemoveAsync(string identifier, CancellationToken cancellation) => await _accessor.ExecuteAsync(_commands.Remove, [
		new Parameter("Namespace", _partition),
		new Parameter("Identifier", identifier)], cancellation) > 0;

	protected override async IAsyncEnumerable<Message> OnGetAsync(string topic, [EnumeratorCancellation]CancellationToken cancellation)
	{
		var parameters = topic == null ?
			new Parameter[]
			{
				new("Namespace", _partition),
				new("Timestamp", DateTimeOffset.UtcNow),
			} :
			[
				new("Namespace", _partition),
				new("Topic", topic),
				new("Timestamp", DateTimeOffset.UtcNow),
			];

		var command = topic == null ? _commands.Get : _commands.GetByTopic;
		await foreach(var model in _accessor.ExecuteAsync<MessageModel>(command, parameters, cancellation).WithCancellation(cancellation))
			yield return model.ToMessage();
	}
	#endregion

	#region 私有方法
	private static DateTime Normalize(DateTime timestamp) => timestamp.Kind switch
	{
		DateTimeKind.Utc => timestamp,
		DateTimeKind.Local => timestamp.ToUniversalTime(),
		_ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
	};
	#endregion
}

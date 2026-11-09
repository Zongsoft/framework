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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public async ValueTask<bool> SetValueAsync<T>(string identifier, T value, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var session = this.GetSession();
		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow
		};

		var response = await session.WriteAsync(
			request,
			[
				new WriteValue()
				{
					NodeId = NodeId.Parse(identifier),
					Value = new DataValue(VariantHelper.CastFromWithReflectionFallback(value), StatusCodes.Good),
					AttributeId = Attributes.Value,
				}
			],
			cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException(string.Format(Properties.Resources.Opc_ValueWriteFailed_Message, response.ResponseHeader.ServiceResult, identifier));

		if(response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0] == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.ToArray().Where(StatusCode.IsBad);

			if(failures.Any())
				throw new InvalidOperationException(string.Format(Properties.Resources.Opc_ValueWriteFailed_Message, string.Join(',', failures), identifier));
		}

		return true;
	}

	public async ValueTask<Failure[]> SetValuesAsync(IEnumerable<KeyValuePair<string, object>> entries, CancellationToken cancellation = default)
	{
		if(entries == null)
			throw new ArgumentNullException(nameof(entries));

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var nodes = entries.Select(entry => new WriteValue()
		{
			NodeId = NodeId.Parse(entry.Key),
			Value = new DataValue(VariantHelper.CastFromWithReflectionFallback(entry.Value)),
		});

		var session = this.GetSession();
		var response = await session.WriteAsync(request, [.. nodes], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException(string.Format(Properties.Resources.Opc_ValuesWriteFailed_Message, response.ResponseHeader.ServiceResult));

		if(response.Results.Count > 0)
			return response.Results.ToArray().Where(StatusCode.IsBad).Select(Failure.GetFailure).ToArray();

		return null;
	}
}

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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
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
					Value = new DataValue(new Variant(value), StatusCodes.Good),
					AttributeId = Attributes.Value,
				}
			],
			cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to write the value of the “{identifier}” node.");

		if(response.Results != null && response.Results.Count > 0)
		{
			//如果失败原因为指定标识不存在则返回失败（不触发异常）
			if(response.Results[0] == StatusCodes.BadNodeIdUnknown)
				return false;

			var failures = response.Results.Where(StatusCode.IsBad);

			if(failures.Any())
				throw new InvalidOperationException($"[{string.Join(',', failures)}] Failed to write the value of the “{identifier}” node.");
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
			Value = new DataValue(new Variant(entry.Value)),
		});

		var session = this.GetSession();
		var response = await session.WriteAsync(request, [.. nodes], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to write node value.");

		if(response.Results != null && response.Results.Count > 0)
			return response.Results.Where(StatusCode.IsBad).Select(Failure.GetFailure).ToArray();

		return null;
	}
}

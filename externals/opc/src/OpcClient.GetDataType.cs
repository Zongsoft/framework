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
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public async ValueTask<Type> GetDataTypeAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var id = NodeId.Parse(identifier);
		var session = this.GetSession();

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var response = await session.ReadAsync(
			request, 0, TimestampsToReturn.Server,
			[
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.DataType,
				},
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.ValueRank,
				},
				new ReadValueId()
				{
					NodeId = id,
					AttributeId = Attributes.ArrayDimensions,
				},
			], cancellation);

		if(response.ResponseHeader != null && StatusCode.IsBad(response.ResponseHeader.ServiceResult))
			throw new InvalidOperationException($"[{response.ResponseHeader.ServiceResult}] Failed to get the data type of the “{identifier}” node.");

		if(response.Results.Count < 2)
			return null;

		var elementType = response.Results[0].GetDataType();
		var rank = response.Results[1].GetValueOrDefault<int>();
		return rank > 0 ? elementType.MakeArrayType(rank) : elementType;
	}
}

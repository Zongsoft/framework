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
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public ValueTask<OpcNodeType> GetDataTypeAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		return GetDataTypeAsync(this.GetSession(), NodeId.Parse(identifier), cancellation);
	}

	private static async ValueTask<OpcNodeType> GetDataTypeAsync(ISession session, NodeId id, CancellationToken cancellation = default)
	{
		var response = await session.ReadAsync(
			default, 0, TimestampsToReturn.Server,
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
			throw new InvalidOperationException(string.Format(Properties.Resources.Opc_DataTypeReadFailed_Message, response.ResponseHeader.ServiceResult, id));

		if(response.Results.Count < 2)
			return null;

		var rank = response.Results[1].WrappedValue.GetInt32();
		return OpcNodeType.Get(response.Results[0].WrappedValue.GetNodeId(), rank);
	}
}

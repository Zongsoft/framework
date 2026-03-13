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
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public async ValueTask<object> GetValueAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			throw new ArgumentNullException(nameof(identifier));

		var id = NodeId.Parse(identifier);
		var session = this.GetSession();
		var result = await ReadValueAsync(session, id, cancellation);

		if(result == null)
			return null;

		if(StatusCode.IsBad(result.StatusCode))
			throw new InvalidOperationException($"[{result.StatusCode}] Failed to read the value of the “{identifier}” node.");

		if(result.Value is ExtensionObject extension)
			return extension.Body;

		return result.Value;

		static async ValueTask<DataValue> ReadValueAsync(Session session, NodeId id, CancellationToken cancellation)
		{
			try
			{
				return await session.ReadValueAsync(id, cancellation);
			}
			catch(ServiceResultException ex) when(ex.StatusCode == StatusCodes.BadNodeIdUnknown)
			{
				return null;
			}
		}
	}

	public async IAsyncEnumerable<KeyValuePair<string, object>> GetValuesAsync(IEnumerable<string> identifiers, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		if(identifiers == null)
			throw new ArgumentNullException(nameof(identifiers));

		var session = this.GetSession();
		NodeId[] nodes = [.. identifiers.Select(NodeId.Parse)];

		(var result, var failures) = await session.ReadValuesAsync(nodes, cancellation);

		if(result.Count != nodes.Length)
			yield break;

		for(int i = 0; i < nodes.Length; i++)
		{
			var entry = result[i];

			if(StatusCode.IsGood(entry.StatusCode))
				yield return new(nodes[i].ToString(), entry.Value is ExtensionObject extension ? extension.Body : entry.Value);
			else
				yield return new(nodes[i].ToString(), new Failure((int)entry.StatusCode.Code, entry.StatusCode.ToString()));
		}
	}
}

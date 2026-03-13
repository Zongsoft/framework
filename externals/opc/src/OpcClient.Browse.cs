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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
	public IAsyncEnumerable<object> BrowseAsync(CancellationToken cancellation = default) => this.BrowseAsync(null, cancellation);
	public IAsyncEnumerable<object> BrowseAsync(string path, CancellationToken cancellation = default)
	{
		return BrowseRecursiveAsync(this.GetSession(), [ GetNodeId(path) ], cancellation);
	}

	private static async IAsyncEnumerable<object> BrowseRecursiveAsync(Session session, IEnumerable<NodeId> nodeIds, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
	{
		if(session == null || nodeIds == null)
			yield break;

		var response = await BrowseAsync(session, nodeIds, cancellation);
		if(response == null)
			yield break;

		foreach(var result in response.Results)
		{
			if(StatusCode.IsGood(result.StatusCode))
			{
				foreach(var reference in result.References)
					yield return reference;

				await foreach(var item in BrowseRecursiveAsync(session, GetNodeIds(result.References), cancellation))
					yield return item;
			}
		}

		static IEnumerable<NodeId> GetNodeIds(ICollection<ReferenceDescription> references) => references
			.Where(reference => !reference.NodeId.IsNull)
			.Select(reference => (NodeId)reference.NodeId);
	}

	private static NodeId GetNodeId(string path)
	{
		if(string.IsNullOrEmpty(path))
			return ObjectIds.ObjectsFolder;
		if(path == "/")
			return ObjectIds.RootFolder;
		if(path.Equals("/Objects", StringComparison.OrdinalIgnoreCase))
			return ObjectIds.ObjectsFolder;
		if(path.Equals("/Types", StringComparison.OrdinalIgnoreCase))
			return ObjectIds.TypesFolder;
		if(path.Equals("/Views", StringComparison.OrdinalIgnoreCase))
			return ObjectIds.ViewsFolder;

		return NodeId.Parse(path);
	}

	private static BrowseDescription GetBrowseArgument(NodeId nodeId, bool includeSubtypes = true) => new()
	{
		NodeId = nodeId,
		BrowseDirection = BrowseDirection.Forward,
		IncludeSubtypes = includeSubtypes,
		NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.Method),
		ResultMask = (uint)BrowseResultMask.All,
	};

	private static Task<BrowseResponse> BrowseAsync(Session session, IEnumerable<NodeId> nodeIds, CancellationToken cancellation)
	{
		if(nodeIds == null || !nodeIds.Any())
			return Task.FromResult<BrowseResponse>(null);

		var arguments = new BrowseDescriptionCollection();
		foreach(var nodeId in nodeIds)
			arguments.Add(GetBrowseArgument(nodeId));

		return arguments.Count > 0 ? session.BrowseAsync(default, default, 0, arguments, cancellation) : null;
	}
}

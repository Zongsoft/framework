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
	#region 公共方法
	public IAsyncEnumerable<OpcNode> BrowseAsync(CancellationToken cancellation = default) => this.BrowseAsync(null, BrowseOptions.Default, cancellation);
	public IAsyncEnumerable<OpcNode> BrowseAsync(BrowseOptions options, CancellationToken cancellation = default) => this.BrowseAsync(null, options, cancellation);
	public IAsyncEnumerable<OpcNode> BrowseAsync(string path, CancellationToken cancellation = default) => this.BrowseAsync(path, BrowseOptions.Default, cancellation);
	public IAsyncEnumerable<OpcNode> BrowseAsync(string path, BrowseOptions options, CancellationToken cancellation = default)
	{
		return BrowseRecursiveAsync(this.GetSession(), options, [GetNodeId(path)], cancellation);

		static NodeId GetNodeId(string path)
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
	}
	#endregion

	#region 私有方法
	private static async IAsyncEnumerable<OpcNode> BrowseRecursiveAsync(Session session, BrowseOptions options, IEnumerable<NodeId> nodeIds, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
	{
		if(session == null || nodeIds == null)
			yield break;

		//确保浏览选项不为空
		options ??= BrowseOptions.Default;

		var response = await BrowseAsync(session, options, nodeIds, cancellation);
		if(response == null)
			yield break;

		foreach(var result in response.Results)
		{
			if(StatusCode.IsNotGood(result.StatusCode))
				continue;

			if(options.Hierarchically)
			{
				bool ignored = !options.IncludeBuiltins;

				foreach(var reference in result.References)
				{
					if(IsIgnored(options, reference))
						continue;

					var node = ToNode(reference, options.Hierarchically);

					if(node.HasChildren(out var nodes))
					{
						var children = BrowseRecursiveAsync(session, options, [(NodeId)reference.NodeId], cancellation);

						await foreach(var child in children)
						{
							if(child.Id.NamespaceIndex == node.Id.NamespaceIndex)
								nodes.Add(child);
						}
					}

					yield return node;
				}
			}
			else
			{
				foreach(var reference in result.References)
				{
					if(!IsIgnored(options, reference))
						yield return ToNode(reference, options.Hierarchically);
				}

				foreach(var chunk in GetNodeIds(result.References).Chunk(1000))
				{
					await foreach(var node in BrowseRecursiveAsync(session, options, chunk, cancellation))
						yield return node;
				}
			}
		}

		static bool IsIgnored(BrowseOptions options, ReferenceDescription reference) =>
			!options.IncludeBuiltins && reference.NodeId.NamespaceIndex == 0;

		static IEnumerable<NodeId> GetNodeIds(ICollection<ReferenceDescription> references) => references
			.Where(reference => !reference.NodeId.IsNull)
			.Select(reference => (NodeId)reference.NodeId);
	}

	private static BrowseDescription GetBrowseArgument(NodeId nodeId, NodeClass @class, bool includeSubtypes = false) => new()
	{
		NodeId = nodeId,
		BrowseDirection = BrowseDirection.Forward,
		IncludeSubtypes = includeSubtypes,
		NodeClassMask = (uint)@class,
		ResultMask = (uint)BrowseResultMask.All,
	};

	private static Task<BrowseResponse> BrowseAsync(Session session, BrowseOptions options, IEnumerable<NodeId> nodeIds, CancellationToken cancellation)
	{
		if(nodeIds == null || !nodeIds.Any())
			return Task.FromResult<BrowseResponse>(null);

		var arguments = new BrowseDescriptionCollection();
		foreach(var nodeId in nodeIds)
			arguments.Add(GetBrowseArgument(nodeId, options.Kind.ToClass(), options.IncludeSubtypes));

		return arguments.Count > 0 ? session.BrowseAsync(default, default, 0, arguments, cancellation) : null;
	}

	private static OpcNode ToNode(ReferenceDescription reference, bool hierarchically)
	{
		return reference.NodeClass == NodeClass.Variable || !hierarchically ?
			new
			(
				(NodeId)reference.NodeId,
				reference.NodeClass.ToKind(),
				OpcNodeType.Get(reference.TypeDefinition),
				reference.BrowseName?.Name,
				reference.DisplayName?.ToString()
			) :
			OpcNode.Hierarchy
			(
				(NodeId)reference.NodeId,
				reference.NodeClass.ToKind(),
				OpcNodeType.Get(reference.TypeDefinition),
				reference.BrowseName?.Name,
				reference.DisplayName?.ToString()
			);
	}
	#endregion

	#region 嵌套子类
	/// <summary>表示浏览选项的类。</summary>
	public class BrowseOptions
	{
		#region 单例字段
		/// <summary>表示默认的浏览选项实例。</summary>
		public static readonly BrowseOptions Default = new(OpcNodeKind.None, true);
		#endregion

		#region 构造函数
		public BrowseOptions(OpcNodeKind kind, bool hierarchically = true)
		{
			this.Kind = kind == OpcNodeKind.None ? OpcNodeKind.Object | OpcNodeKind.Variable : kind;
			this.Hierarchically = hierarchically;
			this.IncludeBuiltins = false;
			this.IncludeSubtypes = false;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置浏览的节点种类。</summary>
		public OpcNodeKind Kind { get; init; }
		/// <summary>获取或设置一个值，指示是否以层次化方式进行浏览。</summary>
		public bool Hierarchically { get; init; }
		/// <summary>获取或设置一个值，指示是否包含内置节点。</summary>
		public bool IncludeBuiltins { get; init; }
		/// <summary>获取或设置一个值，指示是否包含子类型。</summary>
		public bool IncludeSubtypes { get; init; }
		#endregion
	}
	#endregion
}

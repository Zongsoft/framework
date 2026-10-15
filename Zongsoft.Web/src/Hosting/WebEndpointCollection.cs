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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

namespace Zongsoft.Web.Hosting;

/// <summary>提供与服务器配置无关的只读 Web 端点集合及请求范围。</summary>
public sealed class WebEndpointCollection : IReadOnlyList<WebEndpoint>
{
	#region 成员字段
	private readonly WebEndpoint[] _endpoints;
	private readonly WebEndpointProtocol[] _kinds;
	private readonly Dictionary<WebEndpoint, Scope> _scopes = [];
	#endregion

	#region 构造函数
	/// <summary>从逻辑端点及应用协议规则创建只读集合。</summary>
	/// <param name="endpoints">逻辑端点声明。</param>
	/// <param name="protocols">应用协议规则；未指定时仅支持普通 HTTP。</param>
	public WebEndpointCollection(IEnumerable<WebEndpoint> endpoints, IEnumerable<WebEndpointProtocol> protocols = null)
	{
		ArgumentNullException.ThrowIfNull(endpoints);

		var policies = WebEndpointUtility.GetProtocols(protocols);
		var kinds = new Dictionary<string, WebEndpointProtocol>(StringComparer.Ordinal);

		foreach(var policy in policies.Values.Where(policy => policy.Kind != "http"))
		{
			if(!kinds.TryAdd(policy.Kind, policy))
				throw WebEndpointUtility.Invalid(policy.Kind, "DuplicateKind");
		}

		_kinds = kinds.Values.OrderBy(protocol => protocol.Kind, StringComparer.Ordinal).ToArray();
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach(var declaration in endpoints)
		{
			ArgumentNullException.ThrowIfNull(declaration);
			if(!names.Add(declaration.Name))
				throw WebEndpointUtility.Invalid(declaration.Name, "DuplicateEndpoint");

			var declarations = WebEndpointUtility.Normalize(declaration.Protocols, policies, declaration.Name + ":Protocols", true);
			var endpoint = new WebEndpoint(declaration.Name, declaration.Address, declarations);
			var versions = declarations.GroupBy(protocol => policies[protocol.Name ?? "http"].Kind)
				.ToDictionary(group => group.Key, group => group.Select(protocol => protocol.HttpVersion).ToHashSet(), StringComparer.Ordinal);

			_scopes.Add(endpoint, new(versions, [.. declarations.Select(protocol => protocol.Transport)]));
		}

		_endpoints = _scopes.Keys
			.OrderByDescending(endpoint => endpoint.Address.Path.Length)
			.ThenBy(endpoint => endpoint.Name, StringComparer.Ordinal)
			.ToArray();

		foreach(var endpoint in _endpoints)
		{
			foreach(var other in _endpoints)
			{
				if(ReferenceEquals(endpoint, other))
					continue;

				var scope = _scopes[endpoint];
				var otherScope = _scopes[other];

				if(string.Equals(endpoint.Address.Path, other.Address.Path, StringComparison.OrdinalIgnoreCase))
				{
					foreach(var kind in scope.Versions.Keys.Intersect(otherScope.Versions.Keys))
					{
						if(endpoint.Address.Path != other.Address.Path || WebEndpointUtility.SameSocket(endpoint, other) &&
						   SharesTransport(scope.Versions[kind], otherScope.Versions[kind]))
							throw WebEndpointUtility.Invalid(endpoint.Name, "ConflictingRange");
					}
				}

				if(WebEndpointUtility.SameSocket(endpoint, other) && endpoint.Address.Scheme == other.Address.Scheme)
					scope.Transports.UnionWith(other.Protocols.Select(protocol => protocol.Transport));
			}
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取逻辑端点数量。</summary>
	public int Count => _endpoints.Length;
	/// <summary>获取指定索引的逻辑端点。</summary>
	public WebEndpoint this[int index] => _endpoints[index];
	#endregion

	#region 内部方法
	internal bool Supports(WebEndpoint endpoint, string kind, int httpVersion) =>
		_scopes[endpoint].Versions.TryGetValue(kind, out var versions) && versions.Contains(httpVersion);

	internal WebEndpointProtocol SelectProtocol(HttpContext context)
	{
		WebEndpointProtocol result = null;

		foreach(var protocol in _kinds)
		{
			if(!protocol.Matches(context))
				continue;

			if(result != null)
				throw WebEndpointUtility.Invalid("Pipeline", "AmbiguousProtocol");

			result = protocol;
		}

		return result ?? WebEndpointProtocol.Http;
	}

	internal WebEndpoint Find(HttpContext context, string path, string kind, int httpVersion)
	{
		if(string.IsNullOrEmpty(path))
			return null;

		var transport = httpVersion == 3 ? ProtocolType.Udp : ProtocolType.Tcp;

		foreach(var endpoint in _endpoints)
		{
			var scope = _scopes[endpoint];

			if(scope.Versions.ContainsKey(kind) && scope.Transports.Contains(transport) &&
			   endpoint.Contains(context.Connection.LocalIpAddress, context.Connection.LocalPort) &&
			   Matche(endpoint.Address.Path, path))
				return endpoint;
		}

		return null;

		static bool Matche(string prefix, string path) => path.StartsWith(prefix, StringComparison.Ordinal) &&
		(
			prefix.EndsWith('/') ||
			path.Length == prefix.Length ||
			path[prefix.Length] == '/'
		);
	}
	#endregion

	#region 遍历枚举
	/// <summary>获取逻辑端点枚举器。</summary>
	/// <returns>返回只读端点的枚举器。</returns>
	public IEnumerator<WebEndpoint> GetEnumerator() => ((IEnumerable<WebEndpoint>)_endpoints).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _endpoints.GetEnumerator();
	#endregion

	#region 私有方法
	private static bool SharesTransport(HashSet<int> left, HashSet<int> right) =>
		left.Any(version => version is 1 or 2) &&
		right.Any(version => version is 1 or 2) ||
		left.Contains(3) &&
		right.Contains(3);
	#endregion

	#region 嵌套类型
	private sealed class Scope(Dictionary<string, HashSet<int>> versions, HashSet<ProtocolType> transports)
	{
		public readonly Dictionary<string, HashSet<int>> Versions = versions;
		public readonly HashSet<ProtocolType> Transports = transports;
	}
	#endregion
}

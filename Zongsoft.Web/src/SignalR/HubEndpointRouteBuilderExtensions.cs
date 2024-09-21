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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Zongsoft.Web.SignalR
{
	public static class HubEndpointRouteBuilderExtensions
	{
		#region 静态字段
		private static readonly MethodInfo MapMethod = typeof(Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions).GetMethod
		(
			nameof(Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions.MapHub),
			BindingFlags.Public | BindingFlags.Static,
			[typeof(IEndpointRouteBuilder), typeof(string), typeof(Action<HttpConnectionDispatcherOptions>)]
		);
		#endregion

		#region 公共方法
		public static void MapHubs(this IEndpointRouteBuilder endpoints)
		{
			var manager = (ApplicationPartManager)endpoints.ServiceProvider.GetService(typeof(ApplicationPartManager));
			HubFeature feature = new HubFeature();
			manager.PopulateFeature(feature);

			foreach(var hub in feature.Hubs)
			{
				MapHub(endpoints, hub.Type, hub.Pattern);
			}
		}

		public static HubEndpointConventionBuilder MapHub(this IEndpointRouteBuilder endpoints, TypeInfo type, string pattern)
		{
			var map = MapMethod.MakeGenericMethod(type);

			return (HubEndpointConventionBuilder)map.Invoke(null, [endpoints, pattern, (HttpConnectionDispatcherOptions options) =>
			{
				//尝试查找 Hub 类中名为 Options 的静态属性
				var property = type.GetProperty("Options", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

				if(property != null && property.CanRead && property.PropertyType == typeof(HttpConnectionDispatcherOptions))
				{
					var value = (HttpConnectionDispatcherOptions)property.GetValue(null);

					options.MinimumProtocolVersion = value.MinimumProtocolVersion;
					options.ApplicationMaxBufferSize = value.ApplicationMaxBufferSize;
					options.TransportMaxBufferSize = value.TransportMaxBufferSize;
					options.TransportSendTimeout = value.TransportSendTimeout;
					options.TransportSendTimeout = value.TransportSendTimeout;
					options.Transports = value.Transports;
					options.CloseOnAuthenticationExpiration = value.CloseOnAuthenticationExpiration;
					options.LongPolling.PollTimeout = value.LongPolling.PollTimeout;
					options.WebSockets.CloseTimeout = value.WebSockets.CloseTimeout;
					options.WebSockets.SubProtocolSelector = value.WebSockets.SubProtocolSelector;

#if NET8_0_OR_GREATER
					options.AllowStatefulReconnects = value.AllowStatefulReconnects;
#endif

					foreach(var data in value.AuthorizationData)
						options.AuthorizationData.Add(data);
				}
			}]);
		}
		#endregion
	}
}
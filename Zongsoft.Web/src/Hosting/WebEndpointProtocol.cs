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
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

/// <summary>提供 HTTP 承载的应用协议规则；实例在启动后共享，必须保持线程安全。</summary>
public class WebEndpointProtocol
{
	#region 静态字段
	/// <summary>表示不附加应用协议的普通 HTTP 规则。</summary>
	public static readonly WebEndpointProtocol Http = new("http");
	#endregion

	#region 构造函数
	/// <summary>声明与普通 HTTP 共用请求类别的上层协议及默认承载。</summary>
	/// <param name="name">上层协议名称。</param>
	/// <param name="carrier">未指定承载时使用的 HTTP 承载。</param>
	public WebEndpointProtocol(string name, string carrier = "http") : this(name, carrier, "http") { }

	/// <summary>初始化独立请求类别的协议规则。</summary>
	/// <param name="name">上层协议名称。</param>
	/// <param name="carrier">未指定承载时使用的 HTTP 承载。</param>
	/// <param name="kind">请求类别；同类规则共用路径范围。</param>
	protected WebEndpointProtocol(string name, string carrier, string kind)
	{
		this.Name = WebEndpointUtility.GetName(name);
		this.Kind = WebEndpointUtility.GetName(kind);

		if(!NetworkProtocol.TryParse(carrier, out var protocol) || protocol.Name != null || !protocol.IsHttp)
			throw new ArgumentException(null, nameof(carrier));

		this.Carrier = protocol.Carrier;
	}
	#endregion

	#region 公共属性
	/// <summary>获取协议名称。</summary>
	public string Name { get; }
	/// <summary>获取默认 HTTP 承载。</summary>
	public string Carrier { get; }
	/// <summary>获取请求类别。</summary>
	public string Kind { get; }
	#endregion

	#region 公共方法
	/// <summary>规范化该规则负责的声明；返回空值表示不接受。</summary>
	/// <param name="protocol">待处理的协议声明。</param>
	/// <returns>返回规范化声明或空协议；不得改变上层名称。</returns>
	public virtual NetworkProtocol Normalize(NetworkProtocol protocol)
	{
		if((protocol.Name ?? "http") != this.Name || protocol.IsEmpty)
			return default;

		return protocol.Carrier == null ? NetworkProtocol.Parse($"{this.Name}+{this.Carrier}") : protocol;
	}

	/// <summary>识别是否为当前独立类别的请求；普通 HTTP 作为未命中其它类别时的默认类别。</summary>
	/// <param name="context">当前请求上下文。</param>
	/// <returns>如果匹配则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public virtual bool Matches(HttpContext context) => false;

	/// <summary>在选择路径范围之前校验协议请求。</summary>
	/// <param name="context">当前请求上下文。</param>
	/// <returns>返回零表示通过，否则返回 HTTP 错误状态码；可以设置必要的响应头。</returns>
	public virtual int Validate(HttpContext context) => 0;

	/// <summary>在通用范围校验后检查候选路由的协议约束。</summary>
	/// <param name="endpoint">候选路由端点。</param>
	/// <param name="feature">已选定的请求范围。</param>
	/// <returns>如果允许则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public virtual bool Accepts(Endpoint endpoint, WebEndpointFeature feature) => true;

	/// <summary>处理没有匹配范围的请求；默认返回 HTTP 404。</summary>
	/// <param name="context">当前请求上下文。</param>
	/// <returns>返回响应处理任务。</returns>
	public virtual Task RejectAsync(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		context.Response.StatusCode = StatusCodes.Status404NotFound;
		return Task.CompletedTask;
	}
	#endregion
}

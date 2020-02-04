/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Zongsoft.Plugins;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Services.Plugins
{
	/// <summary>
	/// 服务解析器类。
	/// </summary>
	/// <remarks>
	///		<para>服务解析支持如下几种表达式：</para>
	///		<list type="table">
	///			<item>
	///				<term>{srv:}</term>
	///				<description>暂不支持(返回空)。</description>
	///			</item>
	///			<item>
	///				<term>{srv:@}</term>
	///				<description>获取默认服务容器（即返回默认服务容器本身）。</description>
	///			</item>
	///			<item>
	///				<term>{srv:ServiceName@ProviderName}</term>
	///				<description>从指定名称的服务容器中获取指定名称的服务，如果其中一个不存在则返回空。</description>
	///			</item>
	///			<item>
	///				<term>{srv:ServiceName}</term>
	///				<description>
	///					<para>从当前构件所属的服务容器或默认服务容器中，获取指定名称的服务，如果不存在则返回空。</para>
	///					<para>当前构件所属服务容器：是指以当前构件的父节点名称为服务容器名的那个服务容器，如果该名称的服务容器不存在则使用默认服务容器。</para>
	///				</description>
	///			</item>
	///			<item>
	///				<term>{srv:~}</term>
	///				<description>
	///					<para>从当前构件所属的服务容器或默认服务容器中，获取匹配目标成员类型的服务。</para>
	///					<para>当前构件所属服务容器：是指以当前构件的父节点名称为服务容器名的那个服务容器，如果该名称的服务容器不存在则使用默认服务容器。</para>
	///				</description>
	///			</item>
	///			<item>
	///				<term>{srv:*}</term>
	///				<description>
	///					<para>从当前构件所属的服务容器或默认服务容器中，获取匹配目标成员类型的所有服务。</para>
	///					<para>当前构件所属服务容器：是指以当前构件的父节点名称为服务容器名的那个服务容器，如果该名称的服务容器不存在则使用默认服务容器。</para>
	///				</description>
	///			</item>
	///			<item>
	///				<term>{srv:~@}</term>
	///				<description>从默认服务容器中，获取匹配目标成员类型的服务。</description>
	///			</item>
	///			<item>
	///				<term>{srv:*@}</term>
	///				<description>从默认服务容器中，获取匹配目标成员类型的所有服务。</description>
	///			</item>
	///			<item>
	///				<term>{srv:~@ProviderName}</term>
	///				<description>从指定名称的服务容器中，获取匹配目标成员类型的服务。</description>
	///			</item>
	///			<item>
	///				<term>{srv:*@ProviderName}</term>
	///				<description>从指定名称的服务容器中，获取匹配目标成员类型的所有服务。</description>
	///			</item>
	///			<item>
	///				<term>{srv:@ProviderName}</term>
	///				<description>获取指定名称的服务容器（即返回指定名称的服务容器本身）。</description>
	///			</item>
	///		</list>
	///		
	///		<para>注意：所有格式均支持对服务对象的属性或字段进行导航，属性或字段之间使用句点符(.)进行分隔。譬如：{srv:ServiceName.Property.SubProperty@Provider} 或 {srv:~.Property.SubProperty} 等等。</para>
	/// </remarks>
	public class ServicesParser : Parser
	{
		#region 常量定义
		private const string SERVICE_GROUP = "service";
		private const string DELIMITER_GROUP = "delimiter";
		private const string PROVIDER_GROUP = "provider";
		private const string PATTERN = @"\s*(?<" + SERVICE_GROUP + @">[^@,\s]+)?\s*((?<" + DELIMITER_GROUP + @">[@,])\s*(?<" + PROVIDER_GROUP + @">[^@,\s]+)?)?\s*";
		private readonly Regex _regex = new Regex(PATTERN, (RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace));
		#endregion

		#region 重写方法
		public override Type GetValueType(ParserContext context)
		{
			return this.GetValue(context, (provider, mode, isMultiple) =>
			{
				if(mode == null)
					return provider.GetType();

				if(mode.GetType() == typeof(string))
					return provider.GetServiceType((string)mode);

				if(mode is Type)
					return isMultiple ? typeof(IEnumerable<>).MakeGenericType((Type)mode) : (Type)mode;

				return null;
			});
		}

		public override object Parse(ParserContext context)
		{
			return this.GetValue(context, (provider, mode, isMultiple) =>
			{
				if(mode == null)
					return provider;

				if(mode.GetType() == typeof(string))
					return provider.Resolve((string)mode);

				if(mode is Type)
					return isMultiple ? provider.ResolveAll((Type)mode) : provider.Resolve((Type)mode);

				return null;
			});
		}
		#endregion

		#region 私有方法
		private T GetValue<T>(ParserContext context, Func<IServiceProvider, object, bool, T> thunk) where T : class
		{
			if(string.IsNullOrWhiteSpace(context.Text))
				return null;

			//匹配表达式文本
			Match match = _regex.Match(context.Text);

			//解析失败(即无效的表达式)，则返回空
			if(!match.Success)
				return null;

			//根据表达式获取特定的服务容器
			var provider = this.FindServiceProvider(context, match);

			if(provider == null)
				return null;

			//获取表达式指定的服务名
			string serviceName = match.Groups[SERVICE_GROUP].Value;

			//如果服务名为空则返回特定的服务容器
			if(string.IsNullOrWhiteSpace(serviceName))
				return thunk(provider, null, false);

			if(serviceName.StartsWith("~") || serviceName.StartsWith("*"))
			{
				if(context.MemberType == null)
					return null;

				//如果当前目标成员类型为服务容器则返回上面解析到的服务容器对象
				if(typeof(Zongsoft.Services.IServiceProvider).IsAssignableFrom(context.MemberType) || typeof(System.IServiceProvider).IsAssignableFrom(context.MemberType))
					return thunk(provider, null, false);

				//从特定服务容器中获取匹配目标成员类型的服务
				return thunk(provider, context.MemberType, serviceName.StartsWith("*"));
			}

			//返回特定服务容器中指定名称的服务
			return thunk(provider, serviceName, false);
		}

		private IServiceProvider FindServiceProvider(ParserContext context, Match match)
		{
			if(context == null || context.PluginContext.ApplicationContext.Services == null)
				return null;

			if(match == null || (!match.Success))
				return null;

			var serviceFactory = Services.ServiceProviderFactory.Instance;

			//如果指定的服务容器名则返回其指定名称的服务容器
			if(match.Groups[PROVIDER_GROUP].Success)
				return serviceFactory.GetProvider(match.Groups[PROVIDER_GROUP].Value);

			//如果指定了分隔符(服务容器名空)则返回默认服务容器
			if(match.Groups[DELIMITER_GROUP].Success)
				return serviceFactory.Default;

			//获取当前构件的父节点，如果父节点空则返回默认服务容器
			if(context.Node == null || context.Node.Parent == null)
				return serviceFactory.Default;

			//返回以当前构件的父节点名称为服务容器名的那个服务容器，如果该服务容器不存在则返回默认服务容器
			return serviceFactory.GetProvider(context.Node.Parent.Name) ?? serviceFactory.Default;
		}
		#endregion
	}
}

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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins.Web library.
 *
 * The Zongsoft.Plugins.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Plugins;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web
{
	public class WebApplicationContext(IServiceProvider services) : Zongsoft.Plugins.PluginApplicationContext(services)
	{
		#region 成员字段
		private IHttpContextAccessor _http;
		private SessionCollection _session;

		#endregion

		#region 公共属性
		/// <summary>获取当前Web应用程序的上下文对象。</summary>
		public HttpContext HttpContext
		{
			get
			{
				_http ??= this.Services.GetRequiredService<IHttpContextAccessor>();
				return _http.HttpContext;
			}
		}

		public override string ApplicationType => "Web";
		public override ClaimsPrincipal Principal => this.HttpContext?.User;
		public override IDictionary<string, object> Session => _session ??= new SessionCollection(this.Services.GetRequiredService<IHttpContextAccessor>());
		#endregion

		#region 重写方法
		public override bool Initialize()
		{
			if(!base.Initialize())
				return false;

			var manager = this.Services.GetRequiredService<ApplicationPartManager>();

			//获取所有自定义功能提供程序
			var features = this.Services.GetServices<IApplicationFeatureProvider>();

			//添加自定义功能提供程序到应用管理器中
			if(features.Any())
			{
				//查找系统内置的默认控制器提供程序
				var builtin = manager.FeatureProviders.FirstOrDefault(feature => feature is Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider);

				foreach(var feature in features)
				{
					//如果当前功能提供程序是新的控制器提供程序则将内置的控制器提供程序移除
					if(builtin != null && feature is Zongsoft.Web.ControllerFeatureProvider)
						manager.FeatureProviders.Remove(builtin);

					manager.FeatureProviders.Add(feature);
				}
			}

			//尝试加载宿主程序集部件
			if(!manager.ApplicationParts.Any(part => part is AssemblyPart assemblyPart && assemblyPart.Assembly == Assembly.GetEntryAssembly()))
				manager.ApplicationParts.Add(new AssemblyPart(Assembly.GetEntryAssembly()));

			//加载Zongsoft.Plugins.Web程序集部件
			manager.ApplicationParts.Add(new AssemblyPart(this.GetType().Assembly));

			//加载插件中的Web程序集部件
			PopulateApplicationParts(manager.ApplicationParts, this.Plugins);

			//返回初始化成功
			return true;
		}

		protected override IApplicationEnvironment CreateEnvironment(IHostEnvironment environment, IDictionary<object, object> properties) => new WebApplicationEnvironment(environment, properties);
		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? new Workbench(this);
		#endregion

		#region 私有方法
		private static void PopulateApplicationParts(ICollection<ApplicationPart> parts, IEnumerable<Plugin> plugins)
		{
			if(parts == null || plugins == null)
				return;

			foreach(var plugin in plugins)
			{
				var assemblies = plugin.Manifest.Assemblies;

				for(int i = 0; i < assemblies.Count; i++)
				{
					if(IsWebAssembly(assemblies[i]))
						parts.Add(new AssemblyPart(assemblies[i]));
				}

				if(plugin.HasChildren)
					PopulateApplicationParts(parts, plugin.Children);
			}
		}

		private static bool IsWebAssembly(Assembly assembly)
		{
			var references = assembly.GetReferencedAssemblies();

			for(int i = 0; i < references.Length; i++)
			{
				if(references[i].Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
					return true;
			}

			return false;
		}
		#endregion

		#region 嵌套子类
		private class WebApplicationEnvironment : IApplicationEnvironment, IWebEnvironment
		{
			private readonly IHostEnvironment _environment;

			public WebApplicationEnvironment(IHostEnvironment environment, IEnumerable<KeyValuePair<object, object>> properties = null)
			{
				_environment = environment ?? throw new ArgumentNullException(nameof(environment));
				this.Properties = properties == null ? new Dictionary<object, object>() : new Dictionary<object, object>(properties);
			}

			public string Name => _environment.EnvironmentName;
			public IDictionary<object, object> Properties { get; }

			public IWebSite Site => this.Sites?.Default;
			public IWebSiteCollection Sites => ApplicationContext.Current.Configuration.GetOption<Configuration.SiteOptionsCollection>("/Application/Sites");
		}

		private sealed class SessionCollection : IDictionary<string, object>
		{
			#region 成员字段
			private readonly IHttpContextAccessor _accessor;
			private readonly Dictionary<string, object> _dictionary;
			#endregion

			#region 构造函数
			public SessionCollection(IHttpContextAccessor accessor)
			{
				_accessor = accessor;
				_dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			}
			#endregion

			#region 公共属性
			public ICollection<string> Keys
			{
				get
				{
					var routes = _accessor?.HttpContext?.Request?.RouteValues;
					return routes == null ? _dictionary.Keys : _dictionary.Concat(routes).Select(entry => entry.Key).ToArray();
				}
			}

			public ICollection<object> Values
			{
				get
				{
					var routes = _accessor?.HttpContext?.Request?.RouteValues;
					return routes == null ? _dictionary.Values : _dictionary.Concat(routes).Select(entry => entry.Value).ToArray();
				}
			}

			public object this[string name]
			{
				get => this.TryGetValue(name, out var value) ? value : throw new KeyNotFoundException();
				set => _dictionary[name] = value;
			}
			#endregion

			#region 公共方法
			public bool Contains(string name) => this.ContainsKey(name);
			public bool ContainsKey(string name) => _dictionary.ContainsKey(name ?? string.Empty) && _accessor.HttpContext != null && _accessor.HttpContext.Request != null && _accessor.HttpContext.Request.RouteValues.ContainsKey(name ?? string.Empty);

			public void Add(string name, object value) => _dictionary.Add(name, value);
			public bool Remove(string name) => _dictionary.Remove(name);

			public bool TryGetValue(string name, out object value)
			{
				if(_dictionary.TryGetValue(name, out value))
					return true;

				var request = _accessor?.HttpContext?.Request;

				if(!string.IsNullOrEmpty(name) && request != null && request.RouteValues.TryGetValue(name, out value))
					return true;

				value = null;
				return false;
			}
			#endregion

			#region 显式实现
			bool ICollection<KeyValuePair<string, object>>.IsReadOnly => true;
			int ICollection<KeyValuePair<string, object>>.Count => _accessor?.HttpContext?.Request.RouteValues.Count ?? 0;
			void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => throw new NotSupportedException();
			void ICollection<KeyValuePair<string, object>>.Clear() => throw new NotSupportedException();
			bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => this.ContainsKey(item.Key);
			bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => throw new NotSupportedException();
			void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
			{
				if(array == null)
					throw new ArgumentNullException(nameof(array));

				if(arrayIndex < 0 || arrayIndex >= array.Length)
					throw new ArgumentOutOfRangeException(nameof(arrayIndex));

				var iterator = this.GetEnumerator();
				var index = arrayIndex;

				while(iterator.MoveNext() && index++ < array.Length)
				{
					var entry = (KeyValuePair<string, object>)iterator.Current;
					array[index] = entry;
				}
			}
			#endregion

			#region 遍历枚举
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				var request = _accessor?.HttpContext?.Request;

				if(request == null)
					yield break;

				foreach(var entry in _accessor.HttpContext.Request.RouteValues)
					yield return new KeyValuePair<string, object>(entry.Key, entry.Value);
			}
			#endregion
		}
		#endregion
	}
}

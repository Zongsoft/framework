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
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Web
{
	public class WebApplicationContext : Zongsoft.Plugins.PluginApplicationContext
	{
		#region 成员字段
		private IHttpContextAccessor _http;
		private SessionCollection _session;
		#endregion

		#region 构造函数
		public WebApplicationContext(IServiceProvider services) : base(services) { }
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

		/// <inheritdoc />
		public override ClaimsPrincipal Principal => this.HttpContext?.User;

		/// <inheritdoc />
		public override Collections.INamedCollection<object> Session
		{
			get
			{
				_session ??= new SessionCollection(this.Services.GetRequiredService<IHttpContextAccessor>());
				return _session;
			}
		}
		#endregion

		#region 重写方法
		public override bool Initialize()
		{
			if(!base.Initialize())
				return false;

			var parts = this.Services.GetRequiredService<ApplicationPartManager>().ApplicationParts;

			//尝试加载宿主程序集部件
			if(!parts.Any(part => part is AssemblyPart assemblyPart && assemblyPart.Assembly == Assembly.GetEntryAssembly()))
				parts.Add(new AssemblyPart(Assembly.GetEntryAssembly()));

			//加载插件Web程序集部件
			parts.Add(new AssemblyPart(this.GetType().Assembly));

			//加载插件中的Web程序集部件
			PopulateApplicationParts(parts, this.Plugins);

			//返回初始化成功
			return true;
		}

		protected override Plugins.IWorkbenchBase CreateWorkbench(out Plugins.PluginTreeNode node)
		{
			return base.CreateWorkbench(out node) ?? new Workbench(this);
		}
		#endregion

		#region 私有方法
		private static void PopulateApplicationParts(ICollection<ApplicationPart> parts, IEnumerable<Plugins.Plugin> plugins)
		{
			if(parts == null || plugins == null)
				return;

			foreach(var plugin in plugins)
			{
				var assemblies = plugin.Manifest.Assemblies;

				for(int i = 0; i < assemblies.Length; i++)
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
		private class SessionCollection : Zongsoft.Collections.INamedCollection<object>
		{
			#region 成员字段
			private readonly IHttpContextAccessor _accessor;
			#endregion

			#region 构造函数
			public SessionCollection(IHttpContextAccessor accessor) => _accessor = accessor;
			#endregion

			#region 公共属性
			public object this[string name] => this.TryGet(name, out var value) ? value : throw new KeyNotFoundException();
			#endregion

			#region 公共方法
			public bool Contains(string name) => !string.IsNullOrEmpty(name) && _accessor.HttpContext != null && _accessor.HttpContext.Request != null && _accessor.HttpContext.Request.RouteValues.ContainsKey(name);

			public bool TryGet(string name, out object value)
			{
				var request = _accessor?.HttpContext?.Request;

				if(!string.IsNullOrEmpty(name) && request != null && request.RouteValues.TryGetValue(name, out value))
					return true;

				value = null;
				return false;
			}
			#endregion

			#region 显式实现
			bool ICollection<object>.IsReadOnly => true;
			int ICollection<object>.Count => _accessor?.HttpContext?.Request.RouteValues.Count ?? 0;
			void ICollection<object>.Add(object item) => throw new NotSupportedException();
			void ICollection<object>.Clear() => throw new NotSupportedException();
			bool ICollection<object>.Contains(object item) => item switch { string key => this.Contains(key), KeyValuePair<string, object> pair => this.Contains(pair.Key), _ => false };
			bool ICollection<object>.Remove(object item) => throw new NotSupportedException();

			IEnumerable<string> Collections.INamedCollection<object>.Keys => _accessor?.HttpContext?.Request.RouteValues.Keys ?? Array.Empty<string>();
			object Collections.INamedCollection<object>.Get(string name) => this.TryGet(name, out var value) ? value : throw new KeyNotFoundException();
			bool Collections.INamedCollection<object>.Remove(string name) => throw new NotSupportedException();

			void ICollection<object>.CopyTo(object[] array, int arrayIndex)
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
					array[index] = entry.Key;
				}
			}
			#endregion

			#region 遍历枚举
			public IEnumerator<object> GetEnumerator()
			{
				var request = _accessor?.HttpContext?.Request;

				if(request == null)
					yield break;

				foreach(var entry in _accessor.HttpContext.Request.RouteValues)
					yield return new KeyValuePair<string, object>(entry.Key, entry.Value);
			}

			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			#endregion
		}
		#endregion
	}
}

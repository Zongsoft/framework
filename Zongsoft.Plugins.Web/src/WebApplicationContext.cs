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
		/// <summary>
		/// 获取当前Web应用程序的上下文对象。
		/// </summary>
		public HttpContext HttpContext
		{
			get
			{
				if(_http == null)
					_http = this.Services.GetRequiredService<IHttpContextAccessor>();

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
				if(_session == null)
					_session = new SessionCollection(this.Services.GetRequiredService<IHttpContextAccessor>());

				return _session;
			}
		}
		#endregion

		#region 重写方法
		public override void Initialize()
		{
			base.Initialize();

			//加载插件中的Web程序集部件
			PopulateApplicationParts(this.Services.GetRequiredService<ApplicationPartManager>().ApplicationParts, this.Plugins);
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
			public bool Contains(string name) => !string.IsNullOrEmpty(name) && _accessor.HttpContext.Request.RouteValues.ContainsKey(name);

			public bool TryGet(string name, out object value)
			{
				if(!string.IsNullOrEmpty(name) && _accessor.HttpContext.Request.RouteValues.TryGetValue(name, out value))
					return true;

				value = null;
				return false;
			}
			#endregion

			#region 显式实现
			bool ICollection<object>.IsReadOnly => true;
			int ICollection<object>.Count => _accessor.HttpContext.Request.RouteValues.Count;
			void ICollection<object>.Add(object item) => throw new NotSupportedException();
			void ICollection<object>.Clear() => throw new NotSupportedException();
			bool ICollection<object>.Contains(object item) => item switch { string key => this.Contains(key), KeyValuePair<string, object> pair => this.Contains(pair.Key), _ => false };
			bool ICollection<object>.Remove(object item) => throw new NotSupportedException();

			IEnumerable<string> Collections.INamedCollection<object>.Keys => _accessor.HttpContext.Request.RouteValues.Keys;
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
				foreach(var entry in _accessor.HttpContext.Request.RouteValues)
					yield return new KeyValuePair<string, object>(entry.Key, entry.Value);
			}

			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			#endregion
		}
		#endregion
	}
}

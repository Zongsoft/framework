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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Services;

namespace Zongsoft.Plugins.Web.Controllers
{
	[Authorize]
	[ApiController]
	[Route("Plugins")]
	public class PluginController : ControllerBase
	{
		#region 公共方法
		[HttpGet]
		public object Get([FromQuery]string path)
		{
			if(ApplicationContext.Current is not PluginApplicationContext applicationContext)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(path))
				return this.GetPlugins(applicationContext.Plugins);

			var node = applicationContext.PluginTree.Find(path);

			if(node == null)
				return this.NotFound();

			return new PluginNodeDescriptor(node);
		}
		#endregion

		#region 私有方法
		private ICollection<PluginDescriptor> GetPlugins(IEnumerable<Plugin> plugins)
		{
			if(plugins == null)
				return null;

			var result = new List<PluginDescriptor>();

			foreach(var plugin in plugins)
			{
				result.Add(new PluginDescriptor(plugin));
			}

			return result;
		}
		#endregion

		#region 嵌套子类
		private class PluginDescriptor
		{
			#region 构造函数
			public PluginDescriptor(Plugin plugin)
			{
				if(plugin == null)
					throw new ArgumentNullException(nameof(plugin));

				this.Name = plugin.Name;
				this.Status = plugin.Status;
				this.IsHidden = plugin.IsHidden;
				this.IsMaster = plugin.IsMaster;
				this.IsSlave = plugin.IsSlave;

				var root = plugin.PluginTree.Options.ApplicationDirectory;

				if(plugin.FilePath.StartsWith(root))
					this.FilePath = plugin.FilePath[root.Length..];
				else
					this.FilePath = plugin.FilePath;

				if(OperatingSystem.IsWindows())
					this.FilePath = this.FilePath.Replace('\\', '/');

				this.Manifest = new PluginManifestDescriptor(plugin.Manifest);

				if(plugin.HasChildren)
				{
					this.Children = new List<PluginDescriptor>(plugin.Children.Count);

					foreach(var child in plugin.Children)
						this.Children.Add(new PluginDescriptor(child));
				}
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			public string FilePath { get; }
			public PluginStatus Status { get; }
			public bool IsSlave { get; }
			public bool IsMaster { get; }
			public bool IsHidden { get; }
			public PluginManifestDescriptor Manifest { get; }
			public ICollection<PluginDescriptor> Children { get; }
			#endregion
		}

		private class PluginManifestDescriptor
		{
			#region 构造函数
			public PluginManifestDescriptor(Plugin.PluginManifest manifest)
			{
				if(manifest == null)
					throw new ArgumentNullException(nameof(manifest));

				this.Title = manifest.Title;
				this.Author = manifest.Author;
				this.Version = manifest.Version.ToString();

				if(!string.IsNullOrEmpty(manifest.Copyright))
					this.Copyright = manifest.Copyright;

				if(!string.IsNullOrEmpty(manifest.Description))
					this.Description = manifest.Description;

				if(manifest.Dependencies != null && manifest.Dependencies.Count > 0)
					this.Dependencies = manifest.Dependencies.Select(p => p.Name).ToArray();
			}
			#endregion

			#region 公共属性
			public string Title { get; }
			public string Author { get; }
			public string Version { get; }
			public string Copyright { get; }
			public string Description { get; }
			public string[] Dependencies { get; }
			#endregion
		}

		private class PluginNodeDescriptor
		{
			#region 构造函数
			public PluginNodeDescriptor(PluginTreeNode node)
			{
				if(node == null)
					throw new ArgumentNullException(nameof(node));

				this.Name = node.Name;
				this.Path = node.Path;
				this.NodeType = node.NodeType;
				this.Value = node.UnwrapValue(ObtainMode.Never);

				if(node.HasProperties)
					this.Properties = node.Properties.Select(property => new KeyValuePair<string, string>(property.Name, property.RawValue));
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			public string Path { get; }
			public object Value { get; }
			public PluginTreeNodeType NodeType { get; }
			public IEnumerable<KeyValuePair<string, string>> Properties { get; }
			#endregion
		}
		#endregion
	}
}

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
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace Zongsoft.Plugins
{
	internal class PluginResolver
	{
		#region 静态变量
		private static readonly Regex ExtendElementRegex = new Regex(@"\w+(\.\w+)+", RegexOptions.Singleline | RegexOptions.Compiled);
		#endregion

		#region 构造函数
		public PluginResolver(PluginTree pluginTree)
		{
			this.PluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
		}
		#endregion

		#region 公共属性
		public PluginTree PluginTree { get; }
		#endregion

		#region 插件解析
		public void ResolvePluginWithoutManifest(Plugin plugin)
		{
			if(plugin == null)
				throw new ArgumentNullException(nameof(plugin));

			using(XmlReader reader = GetReader(plugin.FilePath))
			{
				if(reader == null)
					return;

				//如果读取插件文件失败则退出
				if(!reader.Read())
					throw new PluginException(string.Format("The '{0}' plugin file cann't read.", plugin.FilePath));

				//跳转到根节点
				reader.MoveToContent();

				//判断插件文件的跟节点名称是否为“plugin”
				if(!string.Equals(reader.Name, "plugin"))
					throw new PluginException(string.Format("This '{0}' plugin file format is invalid.", plugin.FilePath));

				plugin.Manifest.Author = reader.GetAttribute("author");
				plugin.Manifest.Title = reader.GetAttribute("title");
				plugin.Manifest.Version = ParseVersion(reader.GetAttribute("version"));
				plugin.Manifest.Copyright = reader.GetAttribute("copyright");
				plugin.Manifest.Description = reader.GetAttribute("description");

				while(reader.Read())
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.Name)
					{
						case "manifest":
							MoveToEndElement(reader);
							break;
						case "parsers":
							ResolveParsers(reader, plugin);
							break;
						case "builders":
							ResolveBuilders(reader, plugin);
							break;
						case "extension":
							ResolveExtension(reader, plugin);
							break;
						default:
							throw new PluginFileException("Invalid '" + reader.Name + "' of element in this plugin file.");
					}
				}
			}
		}

		public Plugin ResolvePluginManifestOnly(string filePath, Plugin parent)
		{
			using(XmlReader reader = GetReader(filePath))
			{
				if(reader == null)
					return null;

				//如果读取插件文件失败则退出
				if(!reader.Read())
					throw new PluginException(string.Format("The '{0}' plugin file cann't read.", filePath));

				//跳转到根节点
				reader.MoveToContent();

				//判断插件文件的跟节点名称是否为“plugin”
				if(!string.Equals(reader.Name, "plugin"))
					throw new PluginException(string.Format("This '{0}' plugin file format is invalid.", filePath));

				//创建Plugin类实例
				Plugin plugin = new Plugin(PluginTree, reader.GetAttribute("name"), filePath, parent);

				plugin.Manifest.Author = reader.GetAttribute("author");
				plugin.Manifest.Title = reader.GetAttribute("title");
				plugin.Manifest.Version = ParseVersion(reader.GetAttribute("version"));
				plugin.Manifest.Copyright = reader.GetAttribute("copyright");
				plugin.Manifest.Description = reader.GetAttribute("description");

				if(!reader.Read())
					return plugin;

				while(reader.NodeType == XmlNodeType.Element)
				{
					switch(reader.Name)
					{
						case "manifest":
							ResolveManifest(reader, plugin);

							if(reader.NodeType == XmlNodeType.EndElement)
								reader.Read();

							break;
						case "parsers":
							reader.Skip();
							break;
						case "builders":
							reader.Skip();
							break;
						case "extension":
							reader.Skip();
							break;
						default:
							//设置跟踪信息，原因为插件文件中出现无法识别的元素
							throw new PluginException(string.Format("Undefined element '{0}' in this plugin file.", reader.Name));
					}
				}

				return plugin;
			}
		}
		#endregion

		#region 局部解析
		private static void ResolveManifest(XmlReader reader, Plugin plugin)
		{
			if(reader.Name != "manifest")
				return;

			int depth = reader.Depth;

			while(reader.Read() && reader.Depth > depth)
			{
				switch(reader.Name)
				{
					case "assemblies":
						ResolveAssemblies(reader.ReadSubtree(), plugin);
						break;
					case "dependencies":
						if(plugin.IsHidden)
							throw new PluginFileException($"The dependencies cannot be defined in the '{plugin.FilePath}' hidden plugin file.");

						ResolveDependencies(reader.ReadSubtree(), plugin);
						break;
					default:
						throw new PluginFileException("Invalid '" + reader.Name + "' of Manifest element in this plugin file.");
				}
			}
		}

		private static void ResolveAssemblies(XmlReader reader, Plugin plugin)
		{
			if(reader.ReadState == ReadState.Initial)
				reader.Read();

			if(reader.Name == "assemblies")
			{
				if(!reader.ReadToDescendant("assembly"))
					return;
			}

			do
			{
				var value = reader.GetAttribute("optional");
				var optional = !string.IsNullOrEmpty(value) && bool.Parse(value);

				plugin.Manifest.SetAssembly(reader.GetAttribute("name"), optional);
			} while(reader.ReadToNextSibling("assembly"));
		}

		private static void ResolveDependencies(XmlReader reader, Plugin plugin)
		{
			if(reader.ReadState == ReadState.Initial)
				reader.Read();

			if(reader.Name == "dependencies")
			{
				if(!reader.ReadToDescendant("dependency"))
					return;
			}

			do
			{
				plugin.Manifest.Dependencies.SetDependency(reader.GetAttribute("name"));
			} while(reader.ReadToNextSibling("dependency"));
		}

		private static void ResolveParsers(XmlReader reader, Plugin plugin)
		{
			if(reader.Name == "parsers")
				reader.Read();

			if(reader.Name != "parser")
				return;

			do
			{
				plugin.Parsers.Add(reader.GetAttribute("type"), reader.GetAttribute("name"), plugin);
			} while(reader.ReadToNextSibling("parser"));
		}

		private static void ResolveBuilders(XmlReader reader, Plugin plugin)
		{
			if(reader.Name == "builders")
				reader.Read();

			if(reader.Name != "builder")
				return;

			do
			{
				plugin.Builders.Add(reader.GetAttribute("type"), reader.GetAttribute("name"), plugin);
			} while(reader.ReadToNextSibling("builder"));
		}

		private void ResolveExtension(XmlReader reader, Plugin plugin)
		{
			try
			{
				reader = reader.ReadSubtree();
				reader.Read();

				//先读取当前扩展点的路径属性值
				string path = reader.GetAttribute("path");

				//解析当前扩展点元素下的所有构件元素，并生成构件对象
				while(reader.Read())
				{
					if(reader.NodeType == XmlNodeType.None)
						break;

					if(reader.NodeType == XmlNodeType.Element)
					{
						if(IsExtendElement(reader.Name))
							this.ResolveExtendedElement(reader, plugin, path);
						else
							this.ResolveBuiltin(reader, plugin, path);
					}
				}
			}
			finally
			{
				if(reader != null)
					reader.Close();
			}
		}

		private void ResolveExtendedElement(XmlReader reader, Plugin plugin, string path)
		{
			var parts = reader.Name.Split('.');
			var node = PluginTree.EnsurePath(PluginPath.Combine(path, parts[0]));

			if(node == null)
				throw new PluginException($"Invalid '{path}/{parts[0]}' ExtendedElement is not exists in '{plugin.FilePath}' plugin.");

			string propertyName = string.Join(".", parts, 1, parts.Length - 1);

			if(node.NodeType == PluginTreeNodeType.Builtin)
				((Builtin)node.Value).Properties.Set(propertyName, this.ResolveExtendedProperty(reader, plugin, node.FullPath));
			else
				node.Properties.Set(propertyName, this.ResolveExtendedProperty(reader, plugin, node.FullPath), plugin);
		}

		private void ResolveExtendedElement(XmlReader reader, Builtin builtin)
		{
			if(builtin == null)
				throw new ArgumentNullException(nameof(builtin));

			var parts = reader.Name.Split('.');

			if(parts.Length != 2)
				throw new PluginException($"Invalid '{reader.Name}' ExtendElement in '{builtin}'.");

			if(string.Equals(parts[0], builtin.Scheme, StringComparison.OrdinalIgnoreCase))
			{
				switch(parts[1].ToLowerInvariant())
				{
					case "constructor":
						if(builtin.BuiltinType == null)
							throw new PluginException($"This '{reader.Name}' ExtendElement dependencied builtin-type is null.");

						ResolveBuiltinConstructor(reader, builtin.BuiltinType.Constructor);
						break;
					default:
						ResolveBuiltinBehavior(reader, builtin, parts[1]);
						break;
				}
			}
			else if(string.Equals(parts[0], builtin.Name, StringComparison.OrdinalIgnoreCase))
			{
				string propertyName = string.Join(".", parts, 1, parts.Length - 1);
				builtin.Properties.Set(propertyName, this.ResolveExtendedProperty(reader, builtin.Plugin, builtin.FullPath));
			}
			else
			{
				throw new PluginException($"Invalid '{reader.Name}' ExtendElement in '{builtin}'.");
			}
		}

		private object ResolveExtendedProperty(XmlReader reader, Plugin plugin, string path)
		{
			while(reader.Read())
			{
				switch(reader.NodeType)
				{
					case XmlNodeType.Text:
						return reader.Value;
					case XmlNodeType.Element:
						string tempPath = PluginPath.Combine("Temporary", plugin.Name.Replace(".", ""), path.Trim('/').Replace('/', '-'), Zongsoft.Common.Randomizer.GenerateString());
						var tempBuiltin = this.ResolveBuiltin(reader, plugin, tempPath);
						return tempBuiltin;
				}
			}

			return null;
		}

		private Builtin ResolveBuiltin(XmlReader reader, Plugin plugin, string path)
		{
			if(reader == null || reader.NodeType != XmlNodeType.Element)
				return null;

			var builtin = plugin.CreateBuiltin(reader.Name, reader.GetAttribute("name"));

			//设置当前构件的其他属性并将构件对象挂载到插件树中，该方法不会引起读取器的位置或状态变化。
			this.UpdateBuiltin(path, builtin, reader);

			//解析当前构件的内部元素(包括下属的子构件或扩展元素)
			this.ResolveBuiltinContent(reader, builtin);

			return builtin;
		}

		private Builtin ResolveBuiltin(XmlReader reader, Builtin parent)
		{
			if(reader == null || reader.NodeType != XmlNodeType.Element)
				return null;

			Builtin builtin = parent.Plugin.CreateBuiltin(reader.Name, reader.GetAttribute("name"));

			//设置当前构件的其他属性并将构件对象挂载到插件树中，该方法不会引起读取器的位置或状态变化。
			this.UpdateBuiltin(parent.FullPath, builtin, reader);

			//解析当前构件的内部元素(包括下属的子构件或扩展元素)
			this.ResolveBuiltinContent(reader, builtin);

			return builtin;
		}

		private void ResolveBuiltinContent(XmlReader reader, Builtin builtin)
		{
			if(reader.IsEmptyElement)
				return;

			int depth = reader.Depth;

			while(reader.Read() && reader.Depth > depth)
			{
				if(reader.NodeType == XmlNodeType.Element)
				{
					if(IsExtendElement(reader.Name))
						this.ResolveExtendedElement(reader, builtin);
					else
						this.ResolveBuiltin(reader, builtin);
				}
			}
		}

		private void UpdateBuiltin(string path, Builtin builtin, XmlReader reader)
		{
			//循环读取当前构件元素的所有特性，并设置到构件对象的扩展属性集合中
			for(int i = 0; i < reader.AttributeCount; i++)
			{
				reader.MoveToAttribute(i);

				switch(reader.Name.ToLowerInvariant())
				{
					case "name":
						continue;
					case "position":
						builtin.Position = reader.Value;
						break;
					case "type":
						if(builtin.BuiltinType == null)
							builtin.BuiltinType = new BuiltinType(builtin, reader.Value);
						else
							builtin.BuiltinType.TypeName = reader.Value;
						break;
					default:
						builtin.Properties.Set(reader.Name, reader.Value);
						break;
				}
			}

			//将当前读取器的位置重新移回该元素节点上
			if(reader.NodeType == XmlNodeType.Attribute)
				reader.MoveToElement();

			//在插件树中挂载当前构件对象
			this.PluginTree.MountBuiltin(path, builtin);
		}
		#endregion

		#region 私有方法
		private static void ResolveBuiltinConstructor(XmlReader reader, BuiltinTypeConstructor constructor)
		{
			try
			{
				reader = reader.ReadSubtree();
				reader.Read();

				while(reader.Read())
				{
					if(reader.NodeType == XmlNodeType.Element && reader.Name == "parameter")
					{
						var member = constructor.Parameters.Add(reader.GetAttribute("name"), reader.GetAttribute("type"), reader.GetAttribute("value"));

						if(!reader.IsEmptyElement)
						{
							//member.RawValue = reader.ReadString();

							//因为移动框架中的XmlReader不支持ReadString()方法，所以将其改成如下代码
							if(reader.Read() && reader.NodeType == XmlNodeType.Text)
								member.RawValue = reader.Value;
						}
					}
				}
			}
			finally
			{
				if(reader != null)
					reader.Close();
			}
		}

		private static void ResolveBuiltinBehavior(XmlReader reader, Builtin builtin, string behaviorName)
		{
			var behavior = builtin.Behaviors.Add(behaviorName);

			for(int i = 0; i < reader.AttributeCount; i++)
			{
				reader.MoveToAttribute(i);
				behavior.Properties.Set(reader.Name, reader.Value);
			}

			if(reader.NodeType == XmlNodeType.Attribute)
				reader.MoveToElement();

			if(!reader.IsEmptyElement)
			{
				int depth = reader.Depth;

				if(reader.Read() && reader.Depth > depth)
				{
					if(reader.NodeType == XmlNodeType.Text)
						behavior.Text = reader.Value;
					else if(reader.NodeType == XmlNodeType.Element)
						throw new PluginException(string.Format("This '{0}' builtin behavior element be contants child elements in '{1}'.", behaviorName, builtin.ToString()));
				}
			}
		}

		private static XmlReader GetReader(string filePath)
		{
			if(string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));

			if(!File.Exists(filePath))
				throw new FileNotFoundException($"The '{filePath}' file is not exists.");

			XmlReaderSettings settings = new XmlReaderSettings()
			{
				CloseInput = true,
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
				ValidationType = ValidationType.None,
			};

			return XmlReader.Create(filePath, settings);
		}

		private static Version ParseVersion(string version) => version != null && Version.TryParse(version, out var result) ? result : null;

		private static void MoveToEndElement(XmlReader reader)
		{
			if(reader == null || reader.ReadState != ReadState.Interactive || reader.IsEmptyElement)
				return;

			if(reader.NodeType == XmlNodeType.Element)
			{
				int depth = reader.Depth;

				while(reader.Read() && reader.Depth > depth)
					;
			}
		}

		private static bool IsExtendElement(string elementName)
		{
			if(string.IsNullOrWhiteSpace(elementName))
				throw new ArgumentNullException(nameof(elementName));

			if(ExtendElementRegex.IsMatch(elementName))
				return true;

			return false;
		}
		#endregion
	}
}

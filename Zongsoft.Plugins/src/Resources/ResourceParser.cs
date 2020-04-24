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
using System.Resources;
using System.Reflection;
using System.Text.RegularExpressions;

using Zongsoft.Plugins;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Resources
{
	public class ResourceParser : Parser
	{
		#region 常量定义
		private const string PATTERN = @"
(?<name>[\.\w]+)
(
\s+
	(?<argWrapper>
		(
			(?<quote>[""'])
			(?<arg>.+?)
			\k<quote>
		)|(?<arg>[^\s]+)
	)
)*";
		#endregion

		#region 正则表达
		private readonly Regex _regex = new Regex(PATTERN, RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);
		#endregion

		#region 解析方法
		public override object Parse(ParserContext context)
		{
			if(string.IsNullOrWhiteSpace(context.Text))
				return null;

			var match = _regex.Match(context.Text);

			if(!match.Success)
				return context.Text;

			var resourceName = match.Groups["name"].Value;
			var args = new string[match.Groups["arg"].Captures.Count];
			if(args.Length > 0)
				match.Groups["arg"].Captures.CopyTo(args, 0);

			if(TryParse(resourceName, context.Plugin, true, out object result))
				return this.FormatResult(result, args);

			//如果父插件的程序集中未包含指定的资源，则从当前程序集中继续查找
			if(this.GetResourceValue(Assembly.GetExecutingAssembly(), resourceName, out result))
				return this.FormatResult(result, args);

			//如果当前程序集中未包含指定的资源，则从入口主程序集中继续查找
			if(this.GetResourceValue(Assembly.GetEntryAssembly(), resourceName, out result))
				return this.FormatResult(result, args);

			//最后返回无法解析的文本本身
			return context.Text;
		}
		#endregion

		#region 私有方法
		private object FormatResult(object result, string[] args)
		{
			if(result == null)
				return null;

			if(result is string && args != null && args.Length > 0)
				result = string.Format((string)result, (object[])args);

			return result;
		}

		private bool TryParse(string text, Plugin plugin, bool findUp, out object value)
		{
			if(plugin == null)
			{
				value = text;
				return false;
			}

			//在当前插件的程序集中进行资源查找，如果找到则直接返回
			foreach(Assembly assembly in plugin.Manifest.Assemblies)
			{
				if(this.GetResourceValue(assembly, text, out value))
					return true;
			}

			//在当前插件的依赖插件中进行资源查找，如果找到则直接返回
			foreach(var dependency in plugin.Manifest.Dependencies)
			{
				if(TryParse(text, dependency.Plugin, false, out value))
					return true;
			}

			//如果当前插件的程序集中未包含指定的资源，则从父插件中继续查找
			if(findUp)
				return this.TryParse(text, plugin.Parent, findUp, out value);

			//如果所有查找失败则返回假
			value = text;
			return false;
		}

		private bool GetResourceValue(Assembly assembly, string name, out object result)
		{
			result = null;

			if(assembly == null)
				return false;

			//默认资源基名为程序集名
			string baseName = assembly.GetName().Name;

			//定义可选的几种资源基名
			string[] baseNames = new string[]
				{
					baseName,
					baseName + ".Resources",
					baseName + ".Properties.Resources",
				};

			//在指定程序集中从可选的资源基名中找出存在的基名
			baseName = this.GetResourceBaseName(assembly, baseNames);

			//如果在指定的程序集中没有找到合适的资源基名则退出
			if(string.IsNullOrEmpty(baseName))
				return false;

			//创建资源管理对象
			ResourceManager resources = new ResourceManager(baseName, assembly);

			if(resources != null)
				result = resources.GetObject(name);

			return (result != null);
		}

		private string GetResourceBaseName(Assembly assembly, string[] names)
		{
			if(names == null || names.Length < 1)
				return null;

			foreach(string name in names)
			{
				string resourceName = name;

				if(!name.EndsWith(".resources", StringComparison.Ordinal))
					resourceName = name + ".resources";

				//判断指定基名的资源在当前程序集中是否存在，如果不存在直接返回
				if(assembly.GetManifestResourceInfo(resourceName) != null)
					return name;
			}

			return null;
		}
		#endregion
	}
}

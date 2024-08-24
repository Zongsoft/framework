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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.ClosedXml library.
 *
 * The Zongsoft.Externals.ClosedXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.ClosedXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.ClosedXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Service(typeof(IDataTemplateProvider), typeof(IServiceProvider<IDataTemplate>))]
	public class SpreadsheetTemplateProvider : IDataTemplateProvider, IServiceProvider<IDataTemplate>, IMatchable
	{
		#region 单例字段
		public static readonly SpreadsheetTemplateProvider Default = new();
		#endregion

		#region 私有字段
		private bool _initialized;
		private readonly string _root;
		private readonly Dictionary<string, SpreadsheetTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		public SpreadsheetTemplateProvider() : this(null) { }
		public SpreadsheetTemplateProvider(string root) => _root = string.IsNullOrEmpty(root) ?
			ApplicationContext.Current?.ApplicationPath ?? Environment.CurrentDirectory : root;
		#endregion

		#region 初始化器
		private void Initialize()
		{
			if(_initialized)
				return;

			lock(_templates)
			{
				if(_initialized)
					return;

				foreach(var file in Directory.GetFiles(_root, "*.xlsx", SearchOption.AllDirectories))
					_templates.TryAdd(Path.GetFileNameWithoutExtension(file), SpreadsheetTemplate.Create(file));

				_initialized = true;
			}
		}
		#endregion

		#region 公共方法
		public IDataTemplate GetTemplate(string name, string format = null)
		{
			if(!_initialized)
				this.Initialize();

			return _templates.TryGetValue(name, out var template) && (string.IsNullOrEmpty(format) || template.Format.Equals(format)) ? template : null;
		}
		#endregion

		#region 显式实现
		IDataTemplate IServiceProvider<IDataTemplate>.GetService(string name) => this.GetTemplate(name);
		bool IMatchable.Match(object parameter) => parameter switch
		{
			string format => Spreadsheet.Format.Equals(format),
			IDataTemplate template => Spreadsheet.Format.Equals(template.Format),
			_ => false,
		};
		#endregion
	}
}
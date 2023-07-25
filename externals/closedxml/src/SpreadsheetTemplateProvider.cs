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
using System.Threading;
using System.Collections.Concurrent;

using Zongsoft.Services;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Service(typeof(IDataTemplateProvider), typeof(IServiceProvider<IDataTemplate>))]
	public class SpreadsheetTemplateProvider : IDataTemplateProvider, IServiceProvider<IDataTemplate>, IMatchable
	{
		#region 常量定义
		public const string Format = "Spreadsheet";
		#endregion

		#region 单例字段
		public static readonly SpreadsheetTemplateProvider Default = new();
		#endregion

		#region 私有字段
		private volatile int _initialized = 0;
		private readonly string _root;
		private readonly ConcurrentDictionary<string, SpreadsheetTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		public SpreadsheetTemplateProvider(string root = null) => _root = string.IsNullOrEmpty(root) ?
			ApplicationContext.Current?.ApplicationPath ?? Environment.CurrentDirectory : root;
		#endregion

		#region 初始化器
		private void Initialize()
		{
			var initialized = Interlocked.CompareExchange(ref _initialized, 1, 0);
			if(initialized != 0)
				return;

			foreach(var file in Directory.GetFiles(_root, "*.xlsx", SearchOption.AllDirectories))
				_templates.TryAdd(Path.GetFileNameWithoutExtension(file), SpreadsheetTemplate.From(file));
		}
		#endregion

		#region 公共方法
		public IDataTemplate GetTemplate(string name, string format = null)
		{
			if(_initialized == 0)
				this.Initialize();

			return _templates.TryGetValue(name, out var template) && (string.IsNullOrEmpty(format) || string.Equals(format, template.Format, StringComparison.OrdinalIgnoreCase)) ? template : null;
		}
		#endregion

		#region 显式实现
		IDataTemplate IServiceProvider<IDataTemplate>.GetService(string name) => this.GetTemplate(name);
		bool IMatchable.Match(object parameter) => parameter is string format && string.Equals(Format, format, StringComparison.OrdinalIgnoreCase);
		#endregion
	}
}
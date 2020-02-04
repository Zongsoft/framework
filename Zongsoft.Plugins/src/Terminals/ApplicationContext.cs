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
using System.IO;
using System.Reflection;

using Zongsoft.Plugins;

namespace Zongsoft.Terminals.Plugins
{
	public class ApplicationContext : Zongsoft.Plugins.PluginApplicationContext
	{
		#region 静态变量
		public new readonly static ApplicationContext Current = new ApplicationContext();
		#endregion

		#region 成员字段
		private Zongsoft.Options.Configuration.OptionConfiguration _configuration;
		#endregion

		#region 私有构造
		private ApplicationContext() : base("Zongsoft.Terminals.Plugins")
		{
		}
		#endregion

		#region 重写方法
		public override Zongsoft.Options.Configuration.OptionConfiguration Configuration
		{
			get
			{
				if(_configuration == null)
				{
					string filePaht = Path.Combine(this.ApplicationDirectory, Assembly.GetEntryAssembly().GetName().Name) + ".option";

					if(File.Exists(filePaht))
						_configuration = Zongsoft.Options.Configuration.OptionConfiguration.Load(filePaht);
					else
						_configuration = new Zongsoft.Options.Configuration.OptionConfiguration(filePaht);
				}

				return _configuration;
			}
		}

		protected override IWorkbenchBase CreateWorkbench(string[] args)
		{
			PluginTreeNode node = this.PluginContext.PluginTree.Find(this.PluginContext.Settings.WorkbenchPath);

			if(node != null && node.NodeType == PluginTreeNodeType.Builtin)
				return base.CreateWorkbench(args);

			return new Workbench(this);
		}
		#endregion
	}
}

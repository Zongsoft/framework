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
 * This file is part of Zongsoft.Externals.Grapecity library.
 *
 * The Zongsoft.Externals.Grapecity is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Grapecity is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Grapecity library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting
{
	public class ReportParameterCollection : KeyedCollection<string, ReportParameter>, IReportParameterCollection
	{
		#region 构造函数
		public ReportParameterCollection(IEnumerable<ReportParameter> parameters) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(parameters == null)
				return;

			if(this.Items is List<ReportParameter> list)
				list.AddRange(parameters);
			else
			{
				foreach(var parameter in parameters)
					this.Items.Add(parameter);
			}
		}

		public ReportParameterCollection(IEnumerable<GrapeCity.ActiveReports.PageReportModel.ReportParameter> parameters) : base(StringComparer.OrdinalIgnoreCase)
		{
			if(parameters == null)
				return;

			if(this.Items is List<ReportParameter> list)
				list.AddRange(parameters.Select(p => new ReportParameter(p)));
			else
			{
				foreach(var parameter in parameters)
					this.Items.Add(new ReportParameter(parameter));
			}
		}
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(ReportParameter item) => item.Name;
		#endregion

		#region 显式实现
		IReportParameter IReportParameterCollection.this[string name] => this[name];
		bool ICollection<IReportParameter>.IsReadOnly => false;

		void ICollection<IReportParameter>.Add(IReportParameter item)
		{
			if(item == null)
				throw new ArgumentNullException();

			if(item is ReportParameter parameter)
				this.Add(parameter);
			else
				throw new ArgumentException("Invalid parameter type.");
		}

		bool ICollection<IReportParameter>.Contains(IReportParameter item) => this.Contains(item?.Name);
		void ICollection<IReportParameter>.CopyTo(IReportParameter[] array, int arrayIndex)
		{
			if(array == null || array.Length == 0)
				return;

			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			foreach(var item in this.Items)
				array[arrayIndex++] = item;
		}

		IEnumerator<IReportParameter> IEnumerable<IReportParameter>.GetEnumerator() => this.GetEnumerator();
		bool ICollection<IReportParameter>.Remove(IReportParameter item) => this.Remove(item.Name);

		bool IReportParameterCollection.TryGetValue(string name, out IReportParameter parameter)
		{
			var existed = this.TryGetValue(name, out var result);
			parameter = existed ? result : null;
			return existed;
		}
		#endregion
	}
}

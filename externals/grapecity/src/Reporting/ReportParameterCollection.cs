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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Reporting;

namespace Zongsoft.Externals.Grapecity.Reporting;

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
			throw new ArgumentException(Properties.Resources.ReportParameter_InvalidType_Message);
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

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
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataArchiveExtractor))]
	public class SpreadsheetExtractor() : DataArchiveExtractorBase(Spreadsheet.Format.Name, Spreadsheet.Format)
	{
		#region 重写方法
		protected override IDataArchiveReader Open(Stream input, IDataArchiveExtractorOptions options)
		{
			if(input == null)
				throw new ArgumentNullException(nameof(input));
			if(options == null)
				throw new ArgumentNullException(nameof(options));

			var workbook = new XLWorkbook(input);
			if(workbook.Worksheets.Count == 0)
				return null;

			var worksheet = workbook.Worksheets.TryGetWorksheet(options?.Source is string key ? key : options.Model.Name, out var sheet) ? sheet : workbook.Worksheet(1);

			//根据模型名获取指定的数据区引用
			var reference = worksheet.NamedRange(options.Model.Name) ?? worksheet.Workbook.NamedRange(options.Model.Name);
			if(reference == null || !reference.IsValid)
				return null;

			//获取数据内容区
			var dataRange = worksheet.Range(reference.RefersTo);
			if(dataRange == null || dataRange.IsEmpty())
				return null;

			return new DataArchiveReader(worksheet, dataRange);
		}
		#endregion

		#region 嵌套子类
		private sealed class DataArchiveReader : IDataArchiveReader
		{
			private IXLWorksheet _worksheet;
			private IXLRange _data;
			private int _row;
			private readonly int _rows;
			private readonly DataArchiveFieldCollection _fields;

			public DataArchiveReader(IXLWorksheet worksheet, IXLRange data)
			{
				_worksheet = worksheet;
				_data = data;
				_rows = data.RowCount();
				_fields = new DataArchiveFieldCollection(data.ColumnCount());

				foreach(var reference in worksheet.Workbook.NamedRanges.ValidNamedRanges().Concat(worksheet.NamedRanges.ValidNamedRanges()))
				{
					var range = worksheet.Range(reference.RefersTo);

					if(range.RowCount() == 1 && range.ColumnCount() == 1)
					{
						var index = range.FirstColumn().ColumnNumber();

						if(index >= data.FirstColumn().ColumnNumber() && index <= data.LastColumn().ColumnNumber())
							_fields.Add(reference.Name, index - 1);
					}
				}
			}

			public bool IsEmpty => _data.IsEmpty();
			public int FieldCount => _data.ColumnCount();

			public object this[int ordinal] => this.GetValue(ordinal);
			public object this[string name] => this.GetValue(_fields[name].Index);

			public string GetName(int ordinal) => _fields[ordinal]?.Name;
			public object GetValue(string name) => this.GetValue(_fields[name].Index);
			public object GetValue(int ordinal)
			{
				var row = _data.Row(_row);
				var cell = row.Cell(ordinal + 1);

				if(cell == null || cell.Value.IsBlank || cell.Value.IsError || cell.IsEmpty())
					return null;
				else
					return Utility.GetCellValue(cell);
			}

			public T GetValue<T>(string name) => this.GetValue<T>(_fields[name].Index);
			public T GetValue<T>(int ordinal)
			{
				var value = this.GetValue(ordinal);
				return Zongsoft.Common.Convert.ConvertValue<T>(value);
			}

			public bool Read()
			{
				while(_row < _rows && _data.Row(_row + 1).IsEmpty())
				{
					_row++;
				}

				return _row++ < _rows;
			}

			public void Dispose()
			{
				var worksheet = Interlocked.Exchange(ref _worksheet, null);
				if(worksheet != null)
				{
					_data = null;
					worksheet.Workbook?.Dispose();
				}
			}
		}

		private sealed class DataArchiveField
		{
			public DataArchiveField(string name, int index)
			{
				this.Name = name;
				this.Index = index;
			}

			public string Name { get; }
			public int Index { get; }

			public override string ToString() => $"[{this.Index}]{this.Name}";
		}

		private sealed class DataArchiveFieldCollection : IEnumerable<DataArchiveField>
		{
			#region 成员字段
			private readonly DataArchiveField[] _fields;
			private readonly Dictionary<string, int> _names;
			#endregion

			#region 构造函数
			public DataArchiveFieldCollection(int count)
			{
				_fields = new DataArchiveField[count];
				_names = new Dictionary<string, int>(count);
			}
			#endregion

			#region 公共属性
			public DataArchiveField this[int index] => index >= 0 && index < _fields.Length ? _fields[index] : throw new ArgumentOutOfRangeException(nameof(index));
			public DataArchiveField this[string name] => _names.TryGetValue(name, out var index) ? _fields[index] : throw new KeyNotFoundException($"The specified '{name}' column name does not exist.");
			#endregion

			#region 公共方法
			public DataArchiveField Add(string name, int index)
			{
				if(string.IsNullOrEmpty(name))
					throw new ArgumentNullException(nameof(name));
				if(index < 0 || index >= _fields.Length)
					throw new ArgumentOutOfRangeException(nameof(index));

				_names[name] = index;
				var field = new DataArchiveField(name, index);
				_fields[index] = field;
				return field;
			}
			#endregion

			#region 枚举遍历
			IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
			public IEnumerator<DataArchiveField> GetEnumerator()
			{
				for(int i = 0; i < _fields.Length; i++)
					yield return _fields[i];
			}
			#endregion
		}
		#endregion
	}
}
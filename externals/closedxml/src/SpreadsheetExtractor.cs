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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Common;
using Zongsoft.Data;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml;

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
		if(options.Model == null)
			throw new ArgumentException(Properties.Resources.SpreadsheetExtractor_ModelRequired_Message, nameof(options));

		var workbook = new XLWorkbook(input);

		try
		{
			var table = GetTable(workbook, options.Model.Name, options.Source as string);
			return new DataArchiveReader(table, options.Model, GetLastRow(table));
		}
		catch
		{
			workbook.Dispose();
			throw;
		}
	}
	#endregion

	#region 私有方法
	private static IXLTable GetTable(XLWorkbook workbook, string name, string source)
	{
		if(!string.IsNullOrEmpty(source))
		{
			if(!workbook.Worksheets.TryGetWorksheet(source, out var worksheet))
				throw OperationException.Unprocessed(string.Format(Properties.Resources.SpreadsheetExtractor_WorksheetNotFound_Message, source));

			if(worksheet.Tables.TryGetTable(name, out var table))
				return table;
		}
		else
		{
			foreach(var worksheet in workbook.Worksheets)
			{
				if(worksheet.Tables.TryGetTable(name, out var table))
					return table;
			}
		}

		throw OperationException.Unprocessed(string.Format(Properties.Resources.SpreadsheetExtractor_TableNotFound_Message, name));
	}

	private static int GetLastRow(IXLTable table)
	{
		var header = table.HeadersRow();
		var lastRow = table.DataRange?.LastRow().RowNumber() ?? header.RowNumber();

		//总计行表示用户已经明确限定了表格边界，不再自动扩展数据区域
		if(table.ShowTotalsRow)
			return lastRow;

		var firstColumn = table.RangeAddress.FirstAddress.ColumnNumber;
		var lastColumn = table.RangeAddress.LastAddress.ColumnNumber;

		//仅沿表格列向下扩展，以修复用户编辑后表范围没有随数据增长的问题
		for(int column = firstColumn; column <= lastColumn; column++)
		{
			var cell = table.Worksheet.Column(column).LastCellUsed(XLCellsUsedOptions.Contents);
			if(cell != null && cell.Address.RowNumber > lastRow)
				lastRow = cell.Address.RowNumber;
		}

		return lastRow;
	}
	#endregion

	#region 嵌套子类
	private sealed class DataArchiveReader : IDataArchiveReader
	{
		private IXLWorksheet _worksheet;
		private readonly int _headerRow;
		private readonly int _lastRow;
		private readonly int _firstColumn;
		private readonly DataArchiveFieldCollection _fields;
		private int _row;

		public DataArchiveReader(IXLTable table, ModelDescriptor model, int lastRow)
		{
			_worksheet = table.Worksheet;
			_headerRow = table.HeadersRow().RowNumber();
			_lastRow = lastRow;
			_firstColumn = table.RangeAddress.FirstAddress.ColumnNumber;
			_row = _headerRow;
			_fields = new DataArchiveFieldCollection(table.ColumnCount());

			foreach(var reference in _worksheet.DefinedNames.ValidNamedRanges())
			{
				foreach(var range in reference.Ranges)
				{
					if(range.Worksheet != _worksheet || range.RowCount() != 1 || range.ColumnCount() != 1 || range.FirstRow().RowNumber() != _headerRow)
						continue;

					var index = range.FirstColumn().ColumnNumber() - _firstColumn;
					if(index >= 0 && index < _fields.Capacity)
						_fields.Add(reference.Name, index);
				}
			}

			//允许以模型属性名作为表头，以支持不包含字段命名引用的手工数据表。
			for(int index = 0; index < _fields.Capacity; index++)
			{
				if(_fields[index] == null)
				{
					var name = _worksheet.Cell(_headerRow, _firstColumn + index).GetString();
					if(model.Properties.TryGetValue(name, out var property))
						_fields.Add(property.Name, index);
				}
			}

			if(_fields.Count == 0)
				throw OperationException.Unprocessed(string.Format(Properties.Resources.SpreadsheetExtractor_FieldsNotFound_Message, table.Name));
		}

		public bool IsEmpty => _lastRow <= _headerRow;
		public int FieldCount => _fields.Capacity;

		public object this[int ordinal] => this.GetValue(ordinal);
		public object this[string name] => this.GetValue(_fields[name].Index);

		public string GetName(int ordinal) => _fields[ordinal]?.Name;
		public object GetValue(string name) => this.GetValue(_fields[name].Index);
		public object GetValue(int ordinal)
		{
			var cell = _worksheet.Cell(_row, _firstColumn + ordinal);

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
			while(++_row <= _lastRow && _worksheet.Range(_row, _firstColumn, _row, _firstColumn + _fields.Capacity - 1).IsEmpty(XLCellsUsedOptions.Contents)) { }
			return _row <= _lastRow;
		}

		public void Dispose()
		{
			var worksheet = Interlocked.Exchange(ref _worksheet, null);
			if(worksheet != null)
				worksheet.Workbook?.Dispose();
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
			_names = new Dictionary<string, int>(count, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count => _names.Count;
		public int Capacity => _fields.Length;
		public DataArchiveField this[int index] => index >= 0 && index < _fields.Length ? _fields[index] : throw new ArgumentOutOfRangeException(nameof(index));
		public DataArchiveField this[string name] => _names.TryGetValue(name, out var index) ? _fields[index] : throw new KeyNotFoundException(string.Format(Properties.Resources.SpreadsheetExtractor_FieldNotFound_Message, name));
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

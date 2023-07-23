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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataFileResolver))]
	public class SpreadsheetResolver : IDataFileResolver
	{
		#region 公共属性
		public string Format => "Spreadsheet";
		#endregion

		#region 公共方法
		public IAsyncEnumerable<T> ResolveAsync<T>(Stream input, IDataFileTemplate template, CancellationToken cancellation = default) => this.ResolveAsync<T>(input, template, null, cancellation);
		public IAsyncEnumerable<T> ResolveAsync<T>(Stream input, IDataFileTemplate template, IEnumerable<string> fields, CancellationToken cancellation = default)
		{
			if(input == null)
				throw new ArgumentNullException(nameof(input));
			if(template == null)
				throw new ArgumentNullException(nameof(template));

			//注意：如果没有显式指定数据字段，则尝试从工作簿中的命名范围确定数据表中对应的字段
			var descriptors = fields != null && fields.Any() ?
				(ICollection<DataFileField>)fields.Select(field => template.Fields.TryGetValue(field, out var descriptor) ? descriptor : null).Where(descriptor => descriptor != null).ToArray() :
				null;

			var workbook = new XLWorkbook(input);
			return new AsyncEnumerable<T>(workbook.Worksheets.TryGetWorksheet(template.Name, out var sheet) ? sheet : workbook.Worksheet(1), template, descriptors);
		}
		#endregion

		#region 私有方法
		private static T Create<T>() => typeof(T).IsInterface || typeof(T).IsAbstract ? Model.Build<T>() : Activator.CreateInstance<T>();
		#endregion

		#region 嵌套子类
		private class AsyncEnumerable<T> : IAsyncEnumerable<T>
		{
			private readonly IXLWorksheet Worksheet;
			private readonly IDataFileTemplate Template;
			private readonly ICollection<DataFileField> Fields;

            public AsyncEnumerable(IXLWorksheet worksheet, IDataFileTemplate template, ICollection<DataFileField> fields)
            {
				this.Worksheet = worksheet;
				this.Template = template;
				this.Fields = fields;
            }

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => new AsyncEnumerator(this, cancellation);

			private class AsyncEnumerator : IAsyncEnumerator<T>
			{
				private AsyncEnumerable<T> _owner;
				private readonly IXLTable _table;
				private readonly ICollection<TableColumn> _columns;
				private readonly CancellationToken _cancellation;
				private readonly int _rows;
				private int _row;

				public AsyncEnumerator(AsyncEnumerable<T> owner, CancellationToken cancellation)
				{
					_owner = owner;
					_cancellation = cancellation;
					_table = owner.Worksheet.Table(_owner.Template.Name);
					_rows = _table.DataRange.RowCount();

					//如果没有显示指定解析的字段集，则尝试从工作簿的命名范围中进行查找确定
					if(owner.Fields == null || owner.Fields.Count == 0)
					{
						_columns = new List<TableColumn>(_table.Fields.Count());

						//依次从工作簿的命名范围中按顺定位数据表中的字段
						foreach(var namedRange in _owner.Worksheet.NamedRanges)
						{
							if(owner.Template.Fields.TryGetValue(namedRange.Name, out var field) && _table.HeadersRow().Contains(namedRange.RefersTo))
								_columns.Add(new(field, namedRange.Ranges.CellsUsed().FirstOrDefault().WorksheetColumn().ColumnNumber()));
						}
					}
					else
					{
						_columns = new List<TableColumn>(owner.Fields.Count);

						//依次从工作簿的命名范围中按顺定位数据表中的字段
						foreach(var namedRange in _owner.Worksheet.NamedRanges)
						{
							if(owner.Template.Fields.TryGetValue(namedRange.Name, out var field) &&
								_table.HeadersRow().Contains(namedRange.RefersTo) &&
								owner.Fields.Any(field => field.Name == namedRange.Name))
								_columns.Add(new(field, namedRange.Ranges.CellsUsed().FirstOrDefault().WorksheetColumn().ColumnNumber()));
						}
					}


				}

				public T Current
				{
					get
					{
						var row = _table.DataRange.Row(_row);
						var item = Create<T>();

						foreach(var column in _columns)
						{
							var cell = row.Cell(column.Index);
							var value = Utility.GetCellValue(cell, column.Field);

							if(value != null)
								Reflection.Reflector.SetValue(ref item, column.Name, value);
						}

						return item;
					}
				}

				public async ValueTask<bool> MoveNextAsync()
				{
					if(_owner == null || _table == null || _columns == null || _columns.Count == 0)
						return false;

					while(_row++ < _rows)
					{
						if(_cancellation.IsCancellationRequested)
							break;

						var row = _table.DataRange.Row(_row);

						//忽略空行、隐藏行、合并行
						if(row.IsEmpty() || row.IsMerged())
							continue;

						return true;
					}

					await this.DisposeAsync();
					return false;
				}

				public ValueTask DisposeAsync()
				{
					var owner = Interlocked.Exchange(ref _owner, null);
                    owner?.Worksheet.Workbook.Dispose();
					return ValueTask.CompletedTask;
				}
			}
		}

		private readonly struct TableColumn
		{
			public TableColumn(DataFileField field, int index)
			{
				this.Index = index;
				this.Field = field;
			}

			public TableColumn(int index, DataFileField field)
            {
				this.Index = index;
				this.Field = field;
            }

			public string Name => this.Field.Name;
            public readonly int Index;
			public readonly DataFileField Field;

			public override string ToString() => $"[{this.Index}]{this.Field}";
		}
		#endregion
	}
}
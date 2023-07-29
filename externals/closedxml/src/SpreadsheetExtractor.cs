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
using System.Collections.ObjectModel;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataExtractor))]
	public class SpreadsheetExtractor : IDataExtractor, Services.IMatchable
	{
		#region 公共属性
		public string Name => SpreadsheetFormat.Name;
		#endregion

		#region 公共方法
		public IAsyncEnumerable<T> ExtractAsync<T>(Stream input, ModelDescriptor model, CancellationToken cancellation = default) => this.ExtractAsync<T>(input, model, null, cancellation);
		public IAsyncEnumerable<T> ExtractAsync<T>(Stream input, ModelDescriptor model, IDataExtractorOptions options, CancellationToken cancellation = default)
		{
			if(input == null)
				throw new ArgumentNullException(nameof(input));
			if(model == null)
				throw new ArgumentNullException(nameof(model));

			var workbook = new XLWorkbook(input);
			var worksheet = workbook.Worksheets.TryGetWorksheet(options?.Source is string key ? key : model.Name, out var sheet) ? sheet : workbook.Worksheet(1);

			return new AsyncEnumerable<T>(worksheet, model, options?.Fields);
		}
		#endregion

		#region 私有方法
		private static T Create<T>() => typeof(T).IsInterface || typeof(T).IsAbstract ? Model.Build<T>() : Activator.CreateInstance<T>();
		#endregion

		#region 服务匹配
		bool Services.IMatchable.Match(object parameter) => parameter switch
		{
			string format => SpreadsheetFormat.IsFormat(format),
			IDataTemplate template => SpreadsheetFormat.IsFormat(template.Type),
			_ => false,
		};
		#endregion

		#region 嵌套子类
		private class AsyncEnumerable<T> : IAsyncEnumerable<T>
		{
			private readonly IXLWorksheet Worksheet;
			private readonly ModelDescriptor Model;
			private readonly string[] Fields;

			public AsyncEnumerable(IXLWorksheet worksheet, ModelDescriptor model, string[] fields)
			{
				this.Worksheet = worksheet;
				this.Model = model;
				this.Fields = fields;
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => new AsyncEnumerator(this, cancellation);

			private class AsyncEnumerator : IAsyncEnumerator<T>
			{
				private AsyncEnumerable<T> _owner;
				private readonly Table _table;
				private readonly IXLRanges _ignores;
				private readonly CancellationToken _cancellation;
				private int _row;

				public AsyncEnumerator(AsyncEnumerable<T> owner, CancellationToken cancellation)
				{
					_owner = owner;
					_cancellation = cancellation;
					_ignores = (owner.Worksheet.NamedRange("Ignores") ?? owner.Worksheet.Workbook.NamedRange("Ignores"))?.Ranges;
					_table = GetTable(owner.Worksheet, owner.Model, owner.Fields);
				}

				public T Current
				{
					get
					{
						var row = _table.Range.Row(_row);
						var result = Create<T>();

						foreach(var column in _table.Columns)
						{
							var cell = row.Cell(column.Index);
							var value = Utility.GetCellValue(cell, column.Property);

							if(value != null)
								Reflection.Reflector.SetValue(ref result, column.Name, value);
						}

						return result;
					}
				}

				public async ValueTask<bool> MoveNextAsync()
				{
					if(_owner == null || _table == null)
						return false;

					while(_row++ < _table.Rows)
					{
						if(_cancellation.IsCancellationRequested)
							break;

						var row = _table.Range.Row(_row);

						//跳过空行、隐藏行、忽略行
						if(row.IsEmpty() || row.IsMerged() || IsIgnored(_ignores, row))
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

				private static bool IsIgnored(IXLRanges ignores, IXLRangeRow row) => ignores != null && ignores.Contains(row.AsRange());
				private static Table GetTable(IXLWorksheet worksheet, ModelDescriptor model, string[] fields)
				{
					//根据模型名获取指定的数据区引用
					var reference = worksheet.NamedRange(model.Name) ?? worksheet.Workbook.NamedRange(model.Name);
					if(reference == null || !reference.IsValid)
						return null;

					//获取数据内容区
					var dataRange = worksheet.Range(reference.RefersTo);
					if(dataRange == null || dataRange.IsEmpty())
						return null;

					var properties = fields == null || fields.Length == 0 ?
						model.Properties.Where(property => property.Field == null || (property.Field.IsSimplex && !property.Field.Immutable)) :
						fields.Select(field => field != null && model.Properties.TryGetValue(field, out var property) ? property : null).Where(property => property != null);

					var columns = new List<TableColumn>(dataRange.ColumnCount());

					foreach(var property in properties)
					{
						reference = worksheet.NamedRange(property.Name) ?? worksheet.Workbook.NamedRange(property.Name);
						if(reference == null || !reference.IsValid)
							continue;

						var fieldRange = worksheet.Range(reference.RefersTo);
						if(fieldRange == null || fieldRange.IsEmpty())
							continue;

						columns.Add(new(property, fieldRange.FirstColumn().ColumnNumber()));
					}

					return columns == null || columns.Count == 0 ? null : new Table(dataRange, model, columns);
				}
			}
		}

		private sealed class Table
		{
			public Table(IXLRange range, ModelDescriptor model, IEnumerable<TableColumn> columns = null)
			{
				this.Range = range ?? throw new ArgumentNullException(nameof(range));
				this.Model = model ?? throw new ArgumentNullException(nameof(model));
				this.Columns = new TableColumnCollection(this);

				if(columns != null)
				{
					foreach(var column in columns)
					{
						this.Columns.Add(column);
					}
				}

				this.Rows = this.Range.RowCount();
			}

			public int Rows { get; }
			public IXLRange Range { get; }
			public ModelDescriptor Model { get; }
			public TableColumnCollection Columns { get; }
		}

		private sealed class TableColumn
		{
			public TableColumn(ModelPropertyDescriptor property, int index = 0)
			{
				this.Index = index;
				this.Property = property ?? throw new ArgumentNullException(nameof(property));
			}

			public string Name => this.Property.Name;
			public int Index { get; set; }
			public ModelPropertyDescriptor Property { get; }

			public override string ToString() => $"[{this.Index}]{this.Property.Name}";
		}

		private class TableColumnCollection : KeyedCollection<string, TableColumn>
		{
			private readonly Table _table;
			public TableColumnCollection(Table table) : base(StringComparer.OrdinalIgnoreCase) => _table = table;
			protected override string GetKeyForItem(TableColumn column) => column.Name;
		}
		#endregion
	}
}
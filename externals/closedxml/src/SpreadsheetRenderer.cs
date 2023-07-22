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
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Excel;
using ClosedXML.Report;

using Zongsoft.IO;
using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataFileRenderer), typeof(IDataTemplateRenderer))]
	public class SpreadsheetRenderer : IDataFileRenderer, IDataTemplateRenderer
	{
		#region 公共属性
		public string Format => "Spreadsheet";
		#endregion

		#region 数据导出
		public ValueTask RenderAsync(Stream output, IDataFileTemplate template, object data, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, null, cancellation);
		public ValueTask RenderAsync(Stream output, IDataFileTemplate template, object data, IEnumerable<string> fields, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));
			if(template == null)
				throw new ArgumentNullException(nameof(template));

			var descriptors = fields != null && fields.Any() ?
				(IReadOnlyList<DataFileField>)fields.Select(field => template.Fields.TryGetValue(field, out var descriptor) ? descriptor : null).Where(descriptor => descriptor != null).ToArray() :
				template.Fields;

			if(descriptors.Count == 0)
				return ValueTask.CompletedTask;

			using var workbook = new XLWorkbook();
			var worksheet = workbook.AddWorksheet(template.Title ?? template.Name);
			worksheet.Style.Font.SetFontSize(12);
			worksheet.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

			if(descriptors.Count > 5)
				worksheet.PageSetup.SetPageOrientation(XLPageOrientation.Landscape);

			//生成数据文件标题
			worksheet.Cell(1, 1).SetValue(template.Title);
			worksheet.Row(1).Height = 45;
			worksheet.Row(1).Style.Font.SetFontSize(16);
			worksheet.Row(1).Style.Font.SetBold(true);
			worksheet.Row(1).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
			worksheet.Row(1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			var range = worksheet.Range(1, 1, 1, descriptors.Count);
			range.Style.Fill.SetPatternType(XLFillPatternValues.Gray125);
			range.Style.Fill.SetPatternColor(XLColor.Green);
			range.Style.Fill.SetBackgroundColor(XLColor.TeaGreen);
			range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
			range.Style.Border.BottomBorderColor = XLColor.Green;
			range.Merge();

			//生成数据文件时间
			worksheet.Cell(2, 1).SetValue(DateTime.Now);
			worksheet.Row(2).Height = 25;
			worksheet.Row(2).Style.Font.SetFontSize(13);
			worksheet.Row(2).Style.Font.SetItalic(true);
			worksheet.Row(2).Style.Font.SetFontColor(XLColor.Gray);
			worksheet.Row(2).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
			worksheet.Row(2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			range = worksheet.Range(2, 1, 2, descriptors.Count);
			range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
			range.Style.Fill.SetPatternColor(XLColor.White);
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(230, 230, 230));
			range.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
			range.Style.Border.BottomBorderColor = XLColor.CoolBlack;
			range.Merge();

			var index = 1;

			//生成数据字段标题行
			foreach(var descriptor in descriptors)
			{
				//获取字段单元
				var cell = worksheet.Cell(3, index++).AddToNamed(descriptor.Name, XLScope.Worksheet, descriptor.Label);

				//设置字段标题
				cell.SetValue(string.IsNullOrEmpty(descriptor.Label) ? descriptor.Name : descriptor.Label);

				//设置字段标题栏的备注
				if(!string.IsNullOrEmpty(descriptor.Description))
					cell.CreateComment().AddText(descriptor.Description);

				//如果是特定类型则调整其样式
				if(descriptor.Type != null)
				{
					if(!Common.TypeExtension.IsNullable(descriptor.Type, out var type))
						type = descriptor.Type;

					//为枚举列添加说明
					if(type.IsEnum)
					{
						var comment = cell.CreateComment();
						var entries = Common.EnumUtility.GetEnumEntries(type, true);

						for(int i = 0; i < entries.Length; i++)
						{
							if(!string.IsNullOrEmpty(comment.Text))
								comment.AddNewLine();

							if(string.IsNullOrEmpty(entries[i].Description))
								comment.AddText($"[{entries[i].Value}] {entries[i].Name}");
							else
								comment.AddText($"[{entries[i].Value}] {entries[i].Name}:{entries[i].Description}");
						}
					}

					if(type.IsEnum || Common.TypeExtension.IsNumeric(type) || type == typeof(Guid) ||
					   type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
					{
						//所有枚举、数值、日期时间、时间范围等设置其字体
						cell.WorksheetColumn().Style.Font.SetFontName("Arial Narrow");
					}

					//特定类型则设置其水平居中
					if(type.IsEnum || type == typeof(byte) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
						cell.WorksheetColumn().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

					//特定日期时间类型的格式
					if(type == typeof(DateTime) || type == typeof(DateTimeOffset))
						cell.WorksheetColumn().Style.DateFormat.SetFormat("yyyy-MM-dd");
				}
			}

			//设置数据字段标题行样式
			worksheet.Row(3).Height = 30;
			worksheet.Row(3).Style.Font.SetFontSize(13);
			worksheet.Row(3).Style.Font.SetBold(true);
			worksheet.Row(3).Style.Font.SetFontColor(XLColor.Navy);
			worksheet.Row(3).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
			worksheet.Row(3).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
			range = worksheet.Range(3, 1, 3, descriptors.Count);
			range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
			range.Style.Fill.SetPatternColor(XLColor.Orange);
			range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(252, 213, 180));
			range.Style.Border.BottomBorder = XLBorderStyleValues.Double;
			range.Style.Border.BottomBorderColor = XLColor.DarkRed;

			//数据区起始行号
			var row = 4;

			//遍历生成数据区
			if(data is IEnumerable items)
			{
				foreach(var item in items)
					RenderRow(worksheet, row++, item, template, descriptors);
			}
			else
			{
				RenderRow(worksheet, row, data, template, descriptors);
			}

			//创建数据表
			var table = worksheet.Range(3, 1, row, descriptors.Count).CreateTable(template.Name);
			table.ShowRowStripes = false;

			//设置整个内容的外边框
			range = worksheet.Range(1, 1, row, descriptors.Count);
			range.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
			range.Style.Border.OutsideBorderColor = XLColor.CoolBlack;

			//调整各个字段列宽
			worksheet.Columns(1, descriptors.Count).AdjustToContents();

			//写入到输出流
			workbook.SaveAs(output);

			return ValueTask.CompletedTask;
		}

		private void RenderRow(IXLWorksheet worksheet, int row, object item, IDataFileTemplate template, IReadOnlyCollection<DataFileField> fields)
		{
			int index = 1;

			foreach(var field in fields)
			{
				//获取表格单元
				var cell = worksheet.Cell(row, index++);

				//设置字段内容
				if(Reflection.Reflector.TryGetValue(ref item, field.Name, out var value) && value != null)
				{
					if(value.GetType().IsEnum)
						cell.SetCellValue(value.ToString());
					else
						cell.SetCellValue(value);
				}
			}

			worksheet.Row(row).Height = 20;
			var range = worksheet.Range(row, 1, row, fields.Count);
			range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
			range.Style.Border.BottomBorderColor = XLColor.LightGray;

			if(row % 2 == 1)
			{
				range.Style.Fill.SetPatternType(XLFillPatternValues.Gray0625);
				range.Style.Fill.SetPatternColor(XLColor.LightGray);
				range.Style.Fill.SetBackgroundColor(XLColor.FromArgb(240, 240, 240));
			}
		}
		#endregion

		#region 模板渲染
		public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, null, cancellation);
		public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));
			if(template == null)
				throw new ArgumentNullException(nameof(template));

			using var stream = FileSystem.File.Open(template.FilePath);
			using var report = new XLTemplate(stream);

			//添加报表数据
			report.AddVariable(data);

			//添加报表参数
			if(parameters != null)
			{
				foreach(var parameter in parameters)
					report.AddVariable(parameter.Key, parameter.Value);
			}

			//生成报表内容
			report.Generate();
			//输出报表内容
			report.SaveAs(output);

			return ValueTask.CompletedTask;
		}
		#endregion
	}
}
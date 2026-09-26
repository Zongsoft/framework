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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Configuration.Profiles;

public class ProfileOptions
{
	#region 构造函数
	public ProfileOptions(bool preserveBlanks = true)
	{
		this.PreserveBlanks = preserveBlanks;
		this.MaximumDepth = 64;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，指示是否保留空行。</summary>
	public bool PreserveBlanks { get; set; }

	/// <summary>获取或设置一个值，指示导入文件必须存在，默认为 <c>false</c>。</summary>
	/// <remarks>启用时，缺失的直接或递归导入引发包含声明来源的 <see cref="ProfileException"/>。</remarks>
	public bool RequireImports { get; set; }

	/// <summary>获取或设置同时加载的最大文件层数，默认为 <c>64</c>。</summary>
	/// <remarks>根文件计为第一层；设为 <c>1</c> 时只能读取根文件。此选项仅用于读取，不限制关联保存的范围。</remarks>
	/// <exception cref="ArgumentOutOfRangeException">指定的值小于或等于零。</exception>
	public int MaximumDepth
	{
		get => field;
		set => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
	}

	/// <summary>获取或设置导入文件打开且递归检查通过后、解析内容之前的回调。</summary>
	/// <remarks>根文件不触发回调。抛出异常会终止整个加载，不支持跳过当前导入。</remarks>
	public Action<ProfileContext> Importing { get; set; }

	/// <summary>获取或设置导入文件及其递归导入解析成功并合并到引用者之后的回调。</summary>
	/// <remarks>根文件不触发回调。抛出异常会终止整个加载，不回滚已完成的合并和通知。</remarks>
	public Action<ProfileContext> Imported { get; set; }
	#endregion

	#region 内部方法
	//固定本次加载的选项值与委托引用；回调捕获的外部状态仍由调用方管理。
	internal ProfileOptions Clone() => (ProfileOptions)this.MemberwiseClone();
	#endregion
}

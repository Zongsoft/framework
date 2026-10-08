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

namespace Zongsoft.Configuration.Profiles;

/// <summary>提供一条 Profile 指令的处理上下文。</summary>
/// <remarks>前后回调共享本次指令上下文；参数改写不修改原始注释声明。</remarks>
public sealed class ProfileDirectiveContext
{
	#region 构造函数
	internal ProfileDirectiveContext(string name, string argument, Profile profile, ProfileSection section, int lineNumber, int depth, ProfileDirectiveOptions options)
	{
		this.Name = name;
		this.Argument = argument;
		this.Profile = profile;
		this.Section = section;
		this.LineNumber = lineNumber;
		this.Depth = depth;
		this.Behavior = options.Behavior;
		this.Options = options.Clone();
	}
	#endregion

	#region 公共属性
	/// <summary>获取指令名称，保留声明中的大小写。</summary>
	public string Name { get; }
	/// <summary>获取或设置指令参数，移除两端空白；具体语义由指令解释，空值表示空参数。</summary>
	public string Argument { get; set => field = value?.Trim(); }
	/// <summary>获取或设置是否已处理；前置回调设置为真可接管指令，内置处理成功也会设置为真。</summary>
	public bool Handled { get; set; }
	/// <summary>获取本次指令采用的处理行为。</summary>
	public ProfileDirectiveBehavior Behavior { get; }
	/// <summary>获取本条指令的独立选项副本，修改此副本不改变读取器的设置。</summary>
	public ProfileDirectiveOptions Options { get; }
	/// <summary>获取声明本条指令的配置。</summary>
	public Profile Profile { get; }
	/// <summary>获取指令所在章节；根章节为空。</summary>
	public ProfileSection Section { get; }
	/// <summary>获取声明文件的加载路径；匿名输入为空字符串。</summary>
	public string FilePath => this.Profile.FilePath;
	/// <summary>获取指令所在行号，从 <c>1</c> 开始。</summary>
	public int LineNumber { get; }
	/// <summary>获取声明配置的加载层数；根配置为 <c>1</c>。</summary>
	public int Depth { get; }
	#endregion
}

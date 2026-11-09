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

namespace Zongsoft.Configuration.Profiles;

/// <summary>提供配置处理过程中的来源与配置上下文信息。</summary>
/// <remarks>读取前后分别创建上下文；属性引用的配置模型仍可修改。</remarks>
public class ProfileContext
{
	#region 构造函数
	internal ProfileContext(string filePath, int depth, Profile referer, Profile profile = null)
	{
		this.FilePath = filePath ?? string.Empty;
		this.Depth = depth;
		this.Referer = referer;
		this.Profile = profile;
	}
	#endregion

	#region 公共属性
	/// <summary>获取本次读取的绝对加载路径；匿名输入为空字符串。</summary>
	public string FilePath { get; }

	/// <summary>获取当前加载层数；根文件为第一层，直接导入为第二层。</summary>
	public int Depth { get; }

	/// <summary>获取直接引用者；根配置为空。</summary>
	public Profile Referer { get; }

	/// <summary>获取当前配置；读取前为空，指令处理时为正在解析的配置，读取完成后为解析结果。</summary>
	public Profile Profile { get; }
	#endregion
}

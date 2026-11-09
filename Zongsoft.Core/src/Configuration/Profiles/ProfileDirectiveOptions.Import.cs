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

namespace Zongsoft.Configuration.Profiles;

partial class ProfileDirectiveOptions
{
	#region 构建方法
	/// <summary>创建导入指令选项。</summary>
	/// <param name="behavior">指定指令行为。</param>
	/// <param name="maximumDepth">最大加载层数，根文件计一层；零采用内置默认上限 <c>64</c>。</param>
	/// <returns>返回新建的导入指令选项。</returns>
	public static ImportOptions Import(ProfileDirectiveBehavior behavior = ProfileDirectiveBehavior.None, int maximumDepth = 0) => new(behavior, maximumDepth);
	#endregion

	#region 嵌套子类
	/// <summary>表示导入指令的选项。</summary>
	public class ImportOptions : ProfileDirectiveOptions
	{
		#region 常量定义
		internal const string NAME = "import";
		internal const int DEFAULT_MAXIMUM_DEPTH = 64;
		#endregion

		#region 构造函数
		public ImportOptions(ProfileDirectiveBehavior behavior = ProfileDirectiveBehavior.None, int maximumDepth = 0) : base(NAME, behavior) => this.MaximumDepth = maximumDepth;
		#endregion

		#region 公共属性
		/// <summary>获取或设置同时加载的最大层数；根文件计一层，零表示采用内置默认上限 <c>64</c>。</summary>
		/// <exception cref="ArgumentOutOfRangeException">指定的值小于零。</exception>
		public int MaximumDepth
		{
			get => field;
			set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}
		#endregion
	}
	#endregion
}

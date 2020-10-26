/*
 *    _____                                ____
 *   /_   /  ____  ____  ____  ____ ____  / __/_
 *     / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ /_
 *    / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __  __/
 *   /____/\____/_/ /_/\__  /____/\____/_/ / /_
 *                    /____/               \__/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * The MIT License (MIT)
 * 
 * Copyright (C) 2018 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Threading;

namespace Zongsoft.Scheduling.Samples
{
	public class MyHandler : IHandler
	{
		#region 成员字段
		private uint _key;
		#endregion

		#region 构造函数
		public MyHandler(uint key)
		{
			_key = key;
		}
		#endregion

		#region 处理方法
		public void Handle(IHandlerContext context)
		{
			//生成一个随机数
			var random = Math.Abs(Zongsoft.Common.Randomizer.GenerateInt32());

			//随机模拟业务处理发生异常
			if(random % 11 == 0 || random % 97 == 0 || random % 101 == 0)
				throw new InvalidOperationException("This is a mock error in the handler.", new ArgumentOutOfRangeException("unnamed", actualValue:random, null));

			//模拟实际业务处理逻辑（停顿0~1秒）
			Thread.Sleep(random % (1 * 1000));
		}
		#endregion

		#region 重写方法
		public bool Equals(IHandler other)
		{
			if(other == null || other.GetType() != this.GetType())
				return false;

			return ((MyHandler)other)._key == _key;
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as IHandler);
		}

		public override int GetHashCode()
		{
			return (int)_key;
		}

		public override string ToString()
		{
			return this.GetType().Name + "#" + _key.ToString();
		}
		#endregion
	}
}

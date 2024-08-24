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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Aliyun.Pushing
{
	/// <summary>
	/// 表示移动推送的设置选项类。
	/// </summary>
	public class PushingSenderSettings
	{
		#region 成员字段
		private int _expiry;
		private PushingType _type;
		private PushingDeviceType _deviceType;
		private PushingTargetType _targetType;
		#endregion

		#region 构造函数
		public PushingSenderSettings()
		{
			_expiry = 60 * 72;
			_type = PushingType.Message;
			_deviceType = PushingDeviceType.All;
			_targetType = PushingTargetType.Alias;
		}

		public PushingSenderSettings(PushingType type, PushingDeviceType deviceType, PushingTargetType targetType, int expiry = 0)
		{
			_expiry = expiry < 0 ? 60 * 72 : expiry;
			_type = type;
			_deviceType = deviceType;
			_targetType = targetType;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置移动推送消息或通知的过期时间，即当指定的目标不在线的情况下保存的有效期（单位：分钟）。
		/// </summary>
		public int Expiry
		{
			get
			{
				return _expiry;
			}
			set
			{
				_expiry = value;
			}
		}

		/// <summary>
		/// 获取或设置移动推送的类型（消息或通知），默认值为消息(Message)。
		/// </summary>
		public PushingType Type
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
			}
		}

		/// <summary>
		/// 获取或设置移动推送的设备类型，默认值为所有(All)。
		/// </summary>
		public PushingDeviceType DeviceType
		{
			get
			{
				return _deviceType;
			}
			set
			{
				_deviceType = value;
			}
		}

		/// <summary>
		/// 获取或设置移动推送的目标（即推送方式）。
		/// </summary>
		public PushingTargetType TargetType
		{
			get
			{
				return _targetType;
			}
			set
			{
				_targetType = value;
			}
		}
		#endregion
	}
}

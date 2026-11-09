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
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Externals.Grapecity;

public static class Utility
{
	public static byte[] ReadAll(Stream stream)
	{
		if(stream == null)
			return null;

		byte[] buffer;

		if(stream.CanSeek)
		{
			buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);
			return buffer;
		}

		buffer = new byte[1024];
		var data = new List<byte>(1024);
		int bytesRead;

		while((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
		{
			if(bytesRead == buffer.Length)
				data.AddRange(buffer);
			else
			{
				var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
				data.AddRange(span.ToArray());
			}
		}

		return data.ToArray();
	}
}

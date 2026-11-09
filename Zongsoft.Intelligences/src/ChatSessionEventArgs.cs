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

namespace Zongsoft.Intelligences;

public class ChatSessionEventArgs : EventArgs
{
	public ChatSessionEventArgs(IChatService service, IChatSession session)
	{
		this.Service = service ?? throw new ArgumentNullException(nameof(service));
		this.Session = session ?? throw new ArgumentNullException(nameof(session));
	}

	public IChatService Service { get; }
	public IChatSession Session { get; }
}

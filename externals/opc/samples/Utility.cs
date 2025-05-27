using System;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal static class Utility
{
	public static CommandOutletContent AppendValue(this CommandOutletContent content, object value)
	{
		if(value == null)
		{
			content.AppendLine(CommandOutletColor.DarkMagenta, $"<NULL>");
			return content;
		}

		if(value is Failure failure)
		{
			content.Append(CommandOutletColor.DarkRed, $"[{nameof(Failure)}] ");
			content.AppendLine(CommandOutletColor.DarkMagenta, failure.ToString());
			return content;
		}

		if(value is byte[] binary)
		{
			content
				.Append(CommandOutletColor.DarkGreen, System.Convert.ToHexString(binary))
				.AppendType(binary.GetType());

			return content;
		}

		if(value is Array array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				content.Append(CommandOutletColor.DarkGray, $"[{i}] ");
				content.AppendLine(CommandOutletColor.DarkGreen, array.GetValue(i).ToString());
			}

			return content;
		}

		content
			.Append(CommandOutletColor.DarkGreen, value.ToString())
			.AppendType(value.GetType());

		return content;
	}

	private static void AppendType(this CommandOutletContent content, Type type)
	{
		if(type == null)
			return;

		content
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(CommandOutletColor.Yellow, type.GetAlias())
			.AppendLine(CommandOutletColor.DarkGray, ")");
	}
}

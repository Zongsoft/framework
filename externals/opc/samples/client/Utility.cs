using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
			content.AppendLine(CommandOutletColor.DarkGreen, System.Convert.ToHexString(binary));
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
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(CommandOutletColor.Yellow, value.GetType().GetAlias())
			.AppendLine(CommandOutletColor.DarkGray, ")");

		return content;
	}
}

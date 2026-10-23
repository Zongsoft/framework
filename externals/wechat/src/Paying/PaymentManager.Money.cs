using System;

namespace Zongsoft.Externals.Wechat.Paying;

public partial class PaymentManager
{
	private static class Money
	{
		public static int Convert(decimal value, string parameter, bool allowZero = false)
		{
			if(value < 0 || !allowZero && value == 0 || value > int.MaxValue / 100m)
				throw new ArgumentOutOfRangeException(parameter);

			var amount = value * 100m;
			if(decimal.Truncate(amount) != amount)
				throw new ArgumentException(null, parameter);

			return (int)amount;
		}

		public static int Validate(int value, string parameter, bool allowZero = false)
		{
			if(value < 0 || !allowZero && value == 0)
				throw new ArgumentOutOfRangeException(parameter);

			return value;
		}

		public static string Currency(string currency)
		{
			if(string.IsNullOrEmpty(currency) || string.Equals(currency, "CNY", StringComparison.OrdinalIgnoreCase))
				return "CNY";

			throw new ArgumentException(null, nameof(currency));
		}
	}
}

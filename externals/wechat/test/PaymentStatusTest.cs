using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public class PaymentStatusTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("NEW_REMOTE_STATE")]
	public void UnknownStateCannotImplySuccessOrNotPaid(string state)
	{
		Assert.Equal(PaymentStatus.Unknown, PaymentUtility.GetStatus(state));
		Assert.Equal(RefundmentStatus.Unknown, RefundmentUtility.GetStatus(state));
	}

	[Fact]
	public void KnownStatesRetainTheirBusinessMeaning()
	{
		Assert.Equal(PaymentStatus.None, PaymentUtility.GetStatus("NOTPAY"));
		Assert.Equal(PaymentStatus.Paying, PaymentUtility.GetStatus("USERPAYING"));
		Assert.Equal(PaymentStatus.Succeed, PaymentUtility.GetStatus("SUCCESS"));
		Assert.Equal(PaymentStatus.Refund, PaymentUtility.GetStatus("REFUND"));
		Assert.Equal(RefundmentStatus.Processing, RefundmentUtility.GetStatus("PROCESSING"));
		Assert.Equal(RefundmentStatus.Abnormal, RefundmentUtility.GetStatus("ABNORMAL"));
		Assert.Equal(RefundmentStatus.Cancelled, RefundmentUtility.GetStatus("CLOSED"));
	}
}

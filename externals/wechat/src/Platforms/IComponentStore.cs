using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public interface IComponentStore
{
	ValueTask<Component.VerificationTicket> GetTicketAsync(string componentId, CancellationToken cancellation = default);
	ValueTask<bool> CreateAuthorizationAsync(Component.AuthorizationRequest request, CancellationToken cancellation = default);
	ValueTask<Component.Receipt> ReceiveAsync(Component.Notification notification, CancellationToken cancellation = default);
}

using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public interface IComponentAuthorizationStore
{
	ValueTask<Component.AuthorizationState> GetAuthorizationAsync(string componentId, string authorizerAppId, CancellationToken cancellation = default);
	ValueTask<bool> UpdateCredentialAsync(string componentId, string authorizerAppId, string revision, Component.AuthorizationCredential credential, CancellationToken cancellation = default);
}

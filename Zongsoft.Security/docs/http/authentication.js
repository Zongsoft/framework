async function saveCredential(response, environmentName, vscode) {
	const body = response.parsedBody ?? JSON.parse(response.body.toString());
	const credentialId = body.CredentialId ?? body.credentialId;
	const renewalToken = body.RenewalToken ?? body.renewalToken;
	const identity = body.Identity ?? body.identity ?? {};

	if(!credentialId)
		throw new Error("Signin response does not contain CredentialId/credentialId.");

	const signedIn = {
		credentialId,
		renewalToken,
		userId: identity.UserId ?? identity.userId ?? identity.Identifier ?? identity.identifier,
		userName: identity.Name ?? identity.name,
		namespace: identity.Namespace ?? identity.namespace,
		email: identity.Email ?? identity.email,
		phone: identity.Phone ?? identity.phone,
	};

	if(!vscode)
		return signedIn;

	if(!environmentName)
		throw new Error("Current httpYac environment does not define environmentName.");

	const config = vscode.workspace.getConfiguration("httpyac");
	const key = "environmentVariables";
	const inspected = config.inspect(key);
	const variables = JSON.parse(JSON.stringify(config.get(key) ?? { "$shared": {} }));

	variables[environmentName] = Object.assign(variables[environmentName] ?? {}, signedIn);

	const target =
		inspected?.workspaceValue !== undefined ?
		vscode.ConfigurationTarget.Workspace :
		vscode.ConfigurationTarget.Global;

	await config.update(key, variables, target);

	console.info(`signin variables saved to httpYac environment '${environmentName}'.`);
	return signedIn;
}

module.exports = {
	saveCredential,
};

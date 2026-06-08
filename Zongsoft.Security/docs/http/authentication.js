async function saveCredential(response, environmentName, vscode) {
	const body = response.parsedBody ?? JSON.parse(response.body.toString());
	const credentialId = body.CredentialId ?? body.credentialId;
	const renewalToken = body.RenewalToken ?? body.renewalToken;
	const identity = body.Identity ?? body.identity ?? {};

	if(!credentialId)
		throw new Error("Signin response does not contain CredentialId/credentialId.");

	if(!environmentName)
		throw new Error("Current httpYac environment does not define environmentName.");

	const config = vscode.workspace.getConfiguration("httpyac");
	const key = "environmentVariables";
	const inspected = config.inspect(key);
	const variables = JSON.parse(JSON.stringify(config.get(key) ?? { "$shared": {} }));

	variables[environmentName] = variables[environmentName] ?? {};
	variables[environmentName].credentialId = credentialId;
	variables[environmentName].renewalToken = renewalToken;
	variables[environmentName].userId = identity.UserId ?? identity.userId;
	variables[environmentName].userName = identity.Name ?? identity.name;
	variables[environmentName].namespace = identity.Namespace ?? identity.namespace;
	variables[environmentName].email = identity.Email ?? identity.email;
	variables[environmentName].phone = identity.Phone ?? identity.phone;

	const target =
		inspected?.workspaceValue !== undefined ?
		vscode.ConfigurationTarget.Workspace :
		vscode.ConfigurationTarget.Global;

	await config.update(key, variables, target);

	console.info(`signin variables saved to httpYac environment '${environmentName}': ${credentialId}`);
}

module.exports = {
	saveCredential,
};

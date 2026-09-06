# Zongsoft.Intelligences Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Intelligences)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**I**ntelligences](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Intelligences) is the _**AI**_ plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides _**AI**_ integration through the [_**O**llama_](https://ollama.com) protocol stack.

## Install _**O**llama_

On _**W**indows 11_, make sure _WSL 2_ and _**P**odman_ are installed successfully.

> Make sure the _**P**odman_ virtual machine is running. You can verify it with the following commands:
> ```bash
> podman machine list     # List virtual machines
> podman machine info     # Show virtual machine information
> podman machine start    # Start the default virtual machine
> ```

> 💡 **Tip:** _**D**ocker_ and _**P**odman_ use almost the same commands. Replace _`podman`_ with _`docker`_ when using Docker.

### Install _**O**llama_ in _**P**odman_

Follow the _**O**llama_ article _[Ollama is now available as an official Docker image](https://ollama.com/blog/ollama-is-now-available-as-an-official-docker-image)_ and run the following command in _**P**ower**S**hell_ to install the _**O**llama_ image.

```bash
podman run -d -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama
```

> After the download and installation complete, verify it with the following command. The model list is empty at this point:
> ```bash
> podman exec -it ollama ollama list
> ```

## Install a Model

Find the large language model you need in the official _**O**llama_ model library at https://ollama.com/search.

> 💡 For local development and testing, _Qwen (`qwen3:0.6b`)_ is recommended. It is only `523MB` and can run smoothly on low-spec machines without a GPU.

```bash
podman exec -it ollama ollama pull qwen3:0.6b
```

> After the download and installation complete, verify it with the following commands:
> ```bash
> # List local models
> podman exec -it ollama ollama list
>
> # Run the qwen3:0.6b model
> # When it starts successfully, it enters interactive mode. Use /bye to exit.
> podman exec -it ollama ollama run qwen3:0.6b
>
> # List running models
> podman exec -it ollama ollama ps
> ```

## Run and Debug

If the directory that contains `Zongsoft.Intelligences.dll` in the host application also contains a _`preludes`_ subdirectory, new chat sessions look in that directory for a text file whose name starts with the assistant name and use it as the session prelude. See the [ChatSessionPreludeLoader.cs](src/ChatSessionPreludeLoader.cs) source code for details.

This extension library provides both _command-line_ and _**REST**ful **API**_ access.

### Command Line

1. Run the _`deploy.cmd`_ deployment command from the [_terminal_](https://github.com/Zongsoft/hosting/tree/main/terminal) project.
2. Start the compiled local [_terminal_](../../hosting/terminal/) host program.
	> If _**O**llama_ is not installed locally, update the connection address in the _`Zongsoft.Intelligences.option`_ configuration file for this plugin.

#### Commands

- List AI assistants:
	> ```bash
	> ai.assistant
	> ```

- Activate a specific AI assistant:
	> Note: subsequent commands are based on the AI assistant selected by this command.
	> ```bash
	> # Set the AI assistant configuration. The argument is the connection name in the configuration file.
	> ai.assistant ollama
	> ```

- Model commands:
	> ```bash
	> # List local models
	> ai.assistant.model.list
	>
	> # List running models
	> ai.assistant.model.list --running
	>
	> # Show model information
	> ai.assistant.model.info "qwen3:0.6b"
	>
	> # Download and install a model
	> ai.assistant.model.install "qwen3:0.6b"
	> # Delete and uninstall a model
	> ai.assistant.model.uninstall "qwen3:0.6b"
	> ```

- Session commands:
	> ```bash
	> # Create a new session
	> ai.assistant.chat.open
	> # Enter a specific session
	> ai.assistant.chat.open 'session|chatroom'
	>
	> # Close the current session
	> ai.assistant.chat.close
	> # Close a specific session
	> ai.assistant.chat.close 'session|chatroom'
	>
	> # Clear the current session history
	> ai.assistant.chat.clear
	> # Clear a specific session history
	> ai.assistant.chat.clear 'session|chatroom'
	>
	> # Show the current session history
	> ai.assistant.chat.history
	> # Show a specific session history
	> ai.assistant.chat.history 'session|chatroom'
	> ```

- Chat commands:
	> ```bash
	> # Chat
	> ai.assistant.chat "content"
	> # Chat and return plain text
	> ai.assistant.chat --format:text "content"
	>
	> # Chat with asynchronous streaming
	> ai.assistant.chat --streaming "content"
	> # Chat with asynchronous streaming in plain text
	> ai.assistant.chat --streaming --format:text "content"
	>
	> # Enter interactive chat mode
	> ai.assistant.chat --interactive
	> ```

### RESTful API

- Get the AI assistant list:
	> `GET /ai/assistants`
- Get a specific AI assistant:
	> `GET /ai/assistants/{name}`

- Get the model list:
	> `GET /ai/assistants/{name}/models`
- Get model details:
	> `GET /ai/assistants/{name}/models/{id}`

- Get the session list:
	> `GET /ai/assistants/{name}/chats`
- Get session details:
	> `GET /ai/assistants/{name}/chats/{id}`
- Create a new session:
	> `POST /ai/assistants/{name}/chats`
- Close a session:
	> `DELETE /ai/assistants/{name}/chats/{id}`

- Get the chat history of a session:
	> `GET /ai/assistants/{name}/chats/{id}/history`
- Clear the chat history of a session:
	> `DELETE /ai/assistants/{name}/chats/{id}/history`

- Chat without session history:
	> `POST /ai/assistants/{name}/chats/chat`
- Chat with session history:
	> `POST /ai/assistants/{name}/chats/{id}/chat`

> Tip: [_`chat.html`_](./api/chat.html) in the [api](./api/) project is an example for calling the chat _**API**_. It uses [_**S**erver-**S**ent **E**vents_](https://developer.mozilla.org/docs/Web/API/Server-sent_events/Using_server-sent_events).

> For the complete _**API**_, see [_`Zongsoft.Intelligences.Web.http`_](./api/Zongsoft.Intelligences.Web.http) in the [api](./api/) project.
> - `{name}` is the assistant name, such as `ollama`.
> - `{id}` is the session ID.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest registers connection-setting drivers and the `AI` command group. Configure the provider/model before resolving an assistant; add the Web package only on HTTP hosts. The current source `.deploy` still lists older AI dependencies: compare it with the selected package's `.csproj`/lockfile before deployment rather than assuming those versions match the current API.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Intelligences` | [Zongsoft.Intelligences.plugin](src/Zongsoft.Intelligences.plugin) |
| File copying and dependencies | [Zongsoft.Intelligences.deploy](src/Zongsoft.Intelligences.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft intelligences]
nuget:Zongsoft.Intelligences
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Intelligences.option`, `Zongsoft.Intelligences.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

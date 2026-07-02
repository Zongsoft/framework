# Zongsoft.Intelligences 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Intelligences)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Intelligences)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**I**ntelligences](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Intelligences) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的 _**AI**_ 插件库，提供了 [_**O**llama_](https://ollama.com) 协议栈的 _**AI**_ 接入功能。

## 安装 _**O**llama_

在 _**W**indows 11_ 中确保 _WSL 2_ 和 _**P**odman_ 安装成功。

> 确保 _**P**odman_ 虚拟机已经启动，可通过下列命令进行查验：
> ```bash
> podman machine list     #查看虚拟机列表
> podman machine info     #查看虚拟机信息
> podman machine start    #启动默认虚拟机
> ```

> 💡 **提示：** _**D**ocker_ 与 _**P**odman_ 的操作方式基本一致，只需将 _`podman`_ 命令改成 _`docker`_ 即可。

### 在 _**P**odman_ 中安装 _**O**llama_

根据 _**O**llama_ 官方文档 _《[Ollama is now available as an official Docker image](https://ollama.com/blog/ollama-is-now-available-as-an-official-docker-image)》_ 的指引，在 _**P**ower**S**hell_ 中执行下列命令安装 _**O**llama_ 镜像。

```bash
podman run -d -v ollama:/root/.ollama -p 11434:11434 --name ollama ollama/ollama
```

> 等待下载安装完成后，通过下面命令进行查验 _(此时镜像列表为空)_：
> ```bash
> podman exec -it ollama ollama list
> ```

## 安装模型

在 _**O**llama_ 官方模型库 _(https://ollama.com/search)_ 中查找需要的大语言模型。

> 💡 如果只是本地开发测试，推荐使用 _阿里千问 (`qwen3:0.6b`)_ 模型，它只有 `523MB` 大小，可在无显卡的低配电脑上流畅运行。

```bash
podman exec -it ollama ollama pull qwen3:0.6b
```

> 等待下载安装完成后，通过下面命令进行查验：
> ```bash
> # 查看本地模型库列表
> podman exec -it ollama ollama list
> 
> # 运行 qwen3:0.6b 模型
> # 运行成功后自动进入交互模式，可通过 `/bye` 命令退出
> podman exec -it ollama ollama run qwen3:0.6b
> 
> # 查看正在运行的模型列表
> podman exec -it ollama ollama ps
> ```

## 运行调试

如果宿主程序中本库(`Zongsoft.Intelligences.dll`)所在目录中包含一个名为 _`preludes`_ 的子目录，则当开启一个新的聊天会话时默认会在该目录中查找该助手名字打头的文本文件作为新会话的开场白。具体请参考 [ChatSessionPreludeLoader.cs](src/ChatSessionPreludeLoader.cs) 类的源码。

本扩展库提供了 _命令行_ 和 _**REST**ful **API**_ 接口两种方式。

### 命令行

1. 执行 [_terminal_](https://github.com/Zongsoft/hosting/tree/main/terminal) 项目的 _`deploy.cmd`_ 部署命令；
2. 启动编译后的 [_terminal_](../../hosting/terminal/) 本地宿主程序；
	> 如果 _**O**llama_ 没有安装在本机，则需要修改该插件的 _`Zongsoft.Intelligences.option`_ 配置文件中的连接地址。

#### 命令说明

- 查看AI助手列表
	> ```bash
	> ai.assistant
	> ```

- 激活指定的AI助手
	> 注意：后续命令皆基于该命令设置的AI助手。
	> ```bash
	> # 设置AI助手配置（注：该命令参数即为配置文件中的连接名）
	> ai.assistant ollama
	> ```

- 模型命令
	> ```bash
	> # 查看本地模型库列表
	> ai.assistant.model.list
	> 
	> # 查看正在运行的模型列表
	> ai.assistant.model.list --running
	> 
	> # 查看指定的模型信息
	> ai.assistant.model.info "qwen3:0.6b"
	> 
	> # 下载并安装指定的大语言模型
	> ai.assistant.model.install "qwen3:0.6b"
	> # 删除并卸载指定的大语言模型
	> ai.assistant.model.uninstall "qwen3:0.6b"
	> ```

- 会话命令
	> ```bash
	> # 创建一个新的会话
	> ai.assistant.chat.open
	> # 进入指定的会话
	> ai.assistant.chat.open 'session|chatroom'
	> 
	> # 关闭当前会话
	> ai.assistant.chat.close
	> # 关闭指定会话
	> ai.assistant.chat.close 'session|chatroom'
	> 
	> # 清空当前会话的历史记录
	> ai.assistant.chat.clear
	> # 清空指定会话的历史记录
	> ai.assistant.chat.clear 'session|chatroom'
	> 
	> # 查看当前会话的历史纪录
	> ai.assistant.chat.history
	> # 查看指定会话的历史记录
	> ai.assistant.chat.history 'session|chatroom'
	> ```

- 聊天命令
	> ```bash
	> # 对话
	> ai.assistant.chat "内容"
	> # 对话：结果为纯文本
	> ai.assistant.chat --format:text "内容"
	> 
	> # 对话：结果为异步流
	> ai.assistant.chat --streaming "内容"
	> # 对话：结果为纯文本的异步流
	> ai.assistant.chat --streaming --format:text "内容"
	> 
	> # 进入交互对话模式
	> ai.assistant.chat --interactive
	> ```

### RESTful API 接口

- 获取AI助手列表
	> `GET /ai/assistants`
- 获取指定AI助手信息
	> `GET /ai/assistants/{name}`

- 获取模型列表
	> `GET /ai/assistants/{name}/models`
- 获取模型详情
	> `GET /ai/assistants/{name}/models/{id}`

- 获取会话列表
	> `GET /ai/assistants/{name}/chats`
- 获取会话详情
	> `GET /ai/assistants/{name}/chats/{id}`
- 创建新的会话
	> `POST /ai/assistants/{name}/chats`
- 关闭一个会话
	> `DELETE /ai/assistants/{name}/chats/{id}`

- 获取指定会话的聊天历史记录
	> `GET /ai/assistants/{name}/chats/{id}/history`
- 清空指定会话的聊天历史记录
	> `DELETE /ai/assistants/{name}/chats/{id}/history`

- 聊天对话 _（无会话历史）_
	> `POST /ai/assistants/{name}/chats/chat`
- 聊天对话 _（有会话历史）_
	> `POST /ai/assistants/{name}/chats/{id}/chat`

> 提示：[api](./api/) 项目中的 [_`chat.html`_](./api/chat.html) 文件为调用聊天 _**API**_ 的范例，它采用 [_**S**erver-**S**ent **E**vents_](https://developer.mozilla.org/docs/Web/API/Server-sent_events/Using_server-sent_events) 技术实现。

> 完整 _**API**_ 请参考 [api](./api/) 项目中的 [_`Zongsoft.Intelligences.Web.http`_](./api/Zongsoft.Intelligences.Web.http) 文档。
> - `{name}` 表示助手名字，譬如：`ollama`；
> - `{id}`   表示会话编号。

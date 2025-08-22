# 人工智能扩展库

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

> 💡 如果只是本地开发测试，推荐使用 _阿里千问 (`qwen3:0.6b`)_ 模型，它只有 `523MB` 大小，而且在没有显卡的低配电脑上运行流畅、响应快速。

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

本扩展库提供了 _命令行_ 和 **REST**ful _**API**_ 接口两种方式。

### 命令行

1. 执行 [_terminal_](https://github.com/Zongsoft/hosting/tree/main/terminal) 项目的 _`deploy.cmd`_ 部署命令；
2. 启动编译后的 [_terminal_](../../hosting/terminal/) 本地宿主程序；
	> 如果 _**O**llama_ 没有安装在本机，则需要修改该插件的 _`Zongsoft.Intelligences.option`_ 配置文件中的连接地址。

#### 命令说明

- 查看AI助手列表
	> ```bash
	> ai.copilot
	> ```

- 激活指定的AI助手
	> 注意：后续命令皆基于该命令设置的AI助手。
	> ```bash
	> # 设置AI助手配置（注：该命令参数即为配置文件中的连接名）
	> ai.copilot ollama
	> ```

- 模型命令
	> ```bash
	> # 查看本地模型库列表
	> ai.copilot.model.list
	> 
	> # 查看正在运行的模型列表
	> ai.copilot.model.list -running
	> 
	> # 查看指定的模型信息
	> ai.copilot.model.info "qwen3:0.6b"
	> 
	> # 下载并安装指定的大语言模型
	> ai.copilot.model.install "qwen3:0.6b"
	> # 删除并卸载指定的大语言模型
	> ai.copilot.model.uninstall "qwen3:0.6b"
	> ```

- 会话命令
	> ```bash
	> # 创建一个新的会话
	> ai.copilot.chat.open
	> # 进入指定的会话
	> ai.copilot.chat.open 'session|chatroom'
	> 
	> # 关闭当前会话
	> ai.copilot.chat.close
	> # 关闭指定会话
	> ai.copilot.chat.close 'session|chatroom'
	> 
	> # 清空当前会话的历史记录
	> ai.copilot.chat.clear
	> # 清空指定会话的历史记录
	> ai.copilot.chat.clear 'session|chatroom'
	> 
	> # 查看当前会话的历史纪录
	> ai.copilot.chat.history
	> # 查看指定会话的历史记录
	> ai.copilot.chat.history 'session|chatroom'
	> ```

- 聊天命令
	> ```bash
	> # 对话
	> ai.copilot.chat "内容"
	> # 对话：结果为纯文本
	> ai.copilot.chat -format:text "内容"
	> 
	> # 对话：结果为异步流
	> ai.copilot.chat -streaming "内容"
	> # 对话：结果为纯文本的异步流
	> ai.copilot.chat -streaming -format:text "内容"
	> 
	> # 进入交互对话模式
	> ai.copilot.chat -interactive
	> ```

### RESTful API 接口

- 获取AI助手列表
	> `GET /ai/copilots`
- 获取指定AI助手信息
	> `GET /ai/copilots/{key}`

- 获取模型列表
	> `GET /ai/copilots/{key}/models`
- 获取指定的模型信息
	> `GET /ai/copilots/{key}/models/{id}`

- 创建新的会话
	> `POST /ai/copilots/{key}/chats`
- 关闭一个会话
	> `DELETE /ai/copilots/{key}/chats`

- 获取指定会话的聊天历史记录
	> `GET /ai/copilots/{key}/chats/{id}/history`
- 清空指定会话的聊天历史记录
	> `DELETE /ai/copilots/{key}/chats/{id}/history`

- 聊天对话（无会话历史）
	> `POST /ai/copilots/{key}/chats/chat`
- 聊天对话（有会话历史）
	> `POST /ai/copilots/{key}/chats/{id}/chat`

> 提示：[api](./api/) 项目中的 [_`chat.html`_](./api/chat.html) 文件为调用聊天 _**API**_ 的范例，它采用 [_**SSE**_](https://developer.mozilla.org/docs/Web/API/Server-sent_events/Using_server-sent_events) 技术实现。

> 完整信息请参考 [api](./api/) 项目中的 [_`Zongsoft.Intelligences.Web.http`_](./api/Zongsoft.Intelligences.Web.http) 文档。
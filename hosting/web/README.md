## 调试

### 站点绑定

Visual Studio 默认使用 IIS Express 作为Web服务器，且默认只绑定了 `localhost` 的主机名，这就意味着无法通过IP或其他自定义域名进行访问，可通过如下操作添加其他绑定。

在Web宿主项目中的 `.vs` 目录中的 `config` 子目录中，有名为 `applicationhost.config` 配置文件，打开它后，找到如下节点：

```plain
system.applicationHost/sites/site[name=xxxx]/bindings
```

1. 在绑定集中添加一个对应IP或自定义域名的绑定节点，譬如：
```xml
<binding protocol="http" bindingInformation="*:8069:127.0.0.1" />
```

2. 以管理员方式运行“命令终端”，然后在终端执行器中执行下面命令：
> 注意：下面命令中的 `url` 参数值必须以 `/` 结尾，否则命令将执行失败。

```shell
netsh http add urlacl url=http://*:8069/ user=everyone
netsh http show urlacl
```

### 请求限制

IIS Express 服务器默认限制了HTTP的请求内容大小，这会导致在上传较大文件时请求被拒绝，通过如下方式可重置默认限制值。

在Web宿主项目中的 `.vs` 目录中的 `config` 子目录中，有名为 `applicationhost.config` 配置文件，打开它后，找到如下节点：

```plain
system.webServer/security/requestFiltering
```

在该节点下添加如下子节点，假定重新设置请求内容长度限制为：`500MB`
```xml
<requestLimits maxAllowedContentLength="524288000" />
```

然后修改Web宿主项目的 Web.config 文件中的如下配置节：
```xml
<system.web>
	<httpRuntime maxRequestLength="524288000" />
</system.web>
```

### 参考资料

- 《[netsh http 命令](https://learn.microsoft.com/zh-cn/windows-server/networking/technologies/netsh/netsh-http)》
- 《[处理 IIS Express 中的 URL 绑定失败](https://learn.microsoft.com/zh-cn/iis/extensions/using-iis-express/handling-url-binding-failures-in-iis-express)》

## 部署

宿主程序只负责初始化运行时环境，作为插件的承载容器其自身并不含有具体的功能实现，我们通过将需要的插件及其相关附属(配置、证书)文件放置在 `plugins` 目录下的相应子目录中，这个行为即为部署。运行 `deploy.bat` _(Windows)_ 或 `deploy.sh` _(Linux/Unix)_ 脚本以执行由部署文件 _(`*.deploy`)_ 所定义的部署内容。

### 部署文件

通常配置文件与特定的 **部署平台** _（譬如：阿里云、微软Azure、亚马逊AWS）_ 及 **环境** _（譬如：开发环境、测试环境、生产环境）_ 相关，所以应该将这些特定相关性的文件单独存放在 `hosting/.deploy` 目录下的相应子目录中，以便于统一管理与跟踪维护。

#### 配置文件

应该根据配置内容的环境相关性来定义配置文件，相应的环境名作为配置文件名的尾部。下面以 **Zongsoft.Security** 插件的配置文件为例进行说明：

- `Zongsoft.Security.option`
> 表示环境无关的配置文件，其配置作为其他环境有关性配置的缺省值；

- `Zongsoft.Security.test.option`
> 表示**测试环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**测试数据库**并且使用的是**内网地址**等。
- `Zongsoft.Security.production.option`
> 表示**生产环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**生产数据库**并且使用的是**内网地址**等。
- `Zongsoft.Security.development.option`
> 表示**开发环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**开发数据库**并且使用的是**内网地址**等。

- `Zongsoft.Security.debug_test.option`
> 表示**测试环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**测试数据库**并且使用的是**外网地址**等。
- `Zongsoft.Security.debug_production.option`
> 表示**生产环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**生产数据库**并且使用的是**外网地址**等。
- `Zongsoft.Security.debug_development.option`
> 表示**开发环境**有关的配置文件，譬如该配置文件内的数据库连接字符串指向的是**开发数据库**并且使用的是**外网地址**等。


#### 目录结构

关于 `.deploy` 部署目录的大致结构如下：

- `certificates` 证书文件的部署目录
> 注：证书文件一般与部署平台无关，因此该目录下无需再创建相应部署平台的子目录。

- `options` 配置文件的部署目录
	- `azure` 微软云的配置文件目录
	- `aliyun` 阿里云的配置文件目录
	- `amazon` 亚马逊云的配置文件目录


-----

### 注意事项

在运行 `deploy.bat` 脚本之前必须确保 `deploy` 工具已经安装，可通过下面命令查看已安装的全局工具：
```bash
dotnet tool list -g
```

如果尚未安装 `deploy` 工具，可通过下面命令进行全局安装：
```bash
dotnet tool install -g zongsoft.tools.deployer
```

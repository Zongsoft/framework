---
name: zongsoft-reporting
description: 修改或审查 Zongsoft.Reporting 的报表描述符、资源解析、数据加载、参数、仓储与导出契约，并评估 Grapecity 下游兼容性时使用；不用于设计业务报表模板。
---

# Zongsoft.Reporting 实现协作

先阅读 [AGENTS.md](AGENTS.md)；使用者场景、配置与示例见 [README](README.zh-Hans.md)。本技能聚焦实现边界，不替代公开 API 文档。

## 入口与边界

- [IReport](src/IReport.cs) 定义保存、渲染和导出能力；[ReportDescriptor](src/ReportDescriptor.cs) 标识定义并打开流。
- [IReportResourceResolver](src/IReportResourceResolver.cs) 返回资源 `IReportResource`，不是 `IReport` 工厂。勿将模板打开与图片/主题资源解析混为一层。
- [ReportDataLoader](src/ReportDataLoader.cs) 按 `model.Name` 从服务容器 Resolve，要求结果为 `IDataService`，再用模型 Schema 和 `Paging.Page(1)` 调用 SelectAsync；不匹配返回 null。LoadAsync 返回 ValueTask<object>，默认结果为可异步枚举的数据服务查询，取消标记传给查询；调用者等待 LoadAsync 后异步消费结果。
- [ReportDataLocator](src/ReportDataLocator.cs) 当前返回 null，不构成完整定位服务。新增能力不能只更新 README 后假定已有默认实现。
- IReportLocator 与 IReportResourceLocator 的单项查询返回 ValueTask，列表返回 IAsyncEnumerable；IReportRepository 的增改删、IReport.SaveAsync、IReportDescriptor.OpenAsync 及资源 ResolveAsync 均可等待，取消令牌在末尾。FindAsync、CreateAsync、ResolveAsync 保留省略业务选项的令牌便利重载。
- IReportArchiveLocator.OpenAsync 返回 ReportArchiveResult（Archive、Stream）。default 是未找到；无路径时仍返回 Archive；打开的 Stream 归调用方异步释放。文件 URL 转换不承担文件打开或数据查询。
- 格式专属逻辑属于 [Grapecity](../externals/grapecity)，本包不引入 ActiveReports 类型。

GrapeCity 的同步 LocateDataSource/GetData 回调不能调用 LoadAsync 完成异步取数，当前适配器不能针对这一契约编译或直接与其旧二进制混用。其异步预加载和商业运行时接入属于独立适配任务。

## 实现级风险

- `FileReportDescriptor` 的 Key 生成依赖路径与运行时哈希；不要将其作为跨进程持久标识而不验证稳定性。
- 描述符流、资源流、报表实例与导出流是四种不同所有权；变更任一 API 时检查异常和早退释放路径。
- `IReport` 声明能力不代表每个适配器均实现。当前 Grapecity 的直接 Render 抛异常、Export 未完成，Locator setter 未实现；不以接口存在推导生产能力。
- Grapecity `Report.Open(Stream)` 通过 StreamReader 读取并关闭传入流；更改所有权需要同步下游和用户说明。
- 参数名称、类型、空值和多值投射影响模板兼容；数据加载不能绕过 IDataService 的授权。
- 通用 loader 目前固定第一页且不自动遍历全部结果，调整分页是行为变更，需检查内存规模与业务期待。

## 最小验证

- 最小目标 `src/Zongsoft.Reporting.csproj`，本包的接口由直接业务实现验证；GrapeCity 与其 Web 项目暂缓，记录兼容限制，不为其保留同步数据桥。
- 流使用可观测的内存流替身，验证打开失败、读取异常、重复释放；loader 使用记录型 IDataService 核对模型名称、Schema 和分页。
- 报表模板采用无凭据的小型夹具；商业运行时、设计器、字体及许可证需显式准备。
- 不将尚未实现的导出方法写成可用示例，不默认执行商业渲染服务。

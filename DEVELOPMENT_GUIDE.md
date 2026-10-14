# EAP Web API 架构与团队开发规范

本文档用于帮助开发人员快速理解项目结构、调用关系和新功能开发流程。

## 一、核心原则

> Api 负责 HTTP 接入，Application 负责业务编排，Infrastructure 负责技术实现，Contracts 负责对外数据契约。

Controller 不写 SQL，不直接创建数据库连接，不直接调用外部 HTTP 服务，也不编写复杂业务判断。

## 二、项目结构

```text
EAP.WebApi.sln
├── EAP.Models          对外请求、响应和公共数据契约
├── EAP.Application        业务服务、业务规则、抽象接口
├── EAP.DataAccess 数据库连接、SQL、Repository、事务和 Row 模型
├── EAP.Infrastructure     JWT、外部 HTTP 和其它非数据库基础设施
├── EAP.Api                Controller、中间件、启动配置和依赖注入
├── EAP.UnitTests          单元测试
├── EAP.IntegrationTests   API 集成测试
├── Database               数据库脚本
└── Tools                  独立工具
```

## 三、项目依赖关系

```text
EAP.Api
 ├── EAP.Application
 ├── EAP.DataAccess
 ├── EAP.Infrastructure
 └── EAP.Models

EAP.Application
 └── EAP.Models

EAP.DataAccess
 └── EAP.Application

EAP.Infrastructure
 └── EAP.Application

EAP.Models
 └── 不依赖 Api、Application、DataAccess、Infrastructure
```

`EAP.DataAccess -> EAP.Application` 的原因是 Data 项目需要实现 Application 定义的 Repository 接口。`EAP.Infrastructure -> EAP.Application` 的原因是它需要实现外部 HTTP 和 Token 等接口。Api 负责组合这两个实现项目，Application 不反向依赖它们。

当前项目没有 `EAP.Domain`。现有业务主要是 SQL 查询、数据写入和业务流程编排，没有复杂聚合根、值对象或领域状态机，因此暂时不需要单独 Domain 层。

## 四、各层职责

### 1. EAP.Models

放置客户端可见的数据结构：

- Request 请求模型
- Response 响应模型
- DTO
- `ApiResult<T>`
- 分页模型
- 业务返回码
- 中英文资源文件

规则：

- 只描述数据，不写数据库代码。
- 只保留格式校验，例如必填、长度、范围。
- 跨字段校验和业务校验放到 Application。
- 返回给客户端的提示信息必须使用资源文件。

### 2. EAP.Application

放置业务用例和抽象接口：

- `Services`：业务流程编排
- `Interfaces`：Service、Repository、外部服务接口
- `Models`：Application 内部需要的模型
- Mapper、业务校验和统一返回处理

Application 可以依赖 Contracts，但不能依赖 Api 或 Infrastructure。

`Application.Models` 放业务层需要的模型，例如业务服务和 Repository 接口之间传递的模型。这里不放只为 Dapper 映射数据库字段的 Row 类。

### 3. EAP.DataAccess

放置数据库技术实现：

- Dapper Repository
- SQL Server、Oracle、MySQL 数据访问
- 数据库连接工厂和事务
- 存储过程
- SQL Server BulkCopy/MERGE
- 数据库健康检查
- 数据库 Row 模型

Data 只实现 Application 定义的 Repository 接口，不负责完整业务流程。

`EAP.DataAccess.Models` 放数据库查询结果 Row 模型，例如 `ProcessAssemblyRow` 和 `SnDefectDetailRow`。这些类只服务于数据库映射，通常使用 `internal`，不暴露给 Application 或 Api。Repository 查询完成后，必须将 Row 模型映射为 `Application.Models` 后再返回。

### 4. EAP.Infrastructure

放置非数据库基础设施：

- 外部 HTTP API
- JWT Token 生成
- 外部服务配置

Infrastructure 只实现 Application 定义的外部服务接口，不负责完整业务流程。

### 5. EAP.Api

放置系统入口：

- Controller
- Middleware
- Filter
- Swagger
- JWT 验证
- 限流
- 健康检查端点
- 依赖注入和配置注册

Controller 只负责接收请求、调用 Application、返回结果。

## 五、完整调用链

```text
客户端
  -> Controller
  -> Application Service
  -> Application Repository 接口
  -> DataAccess Repository
  -> 数据库
```

外部 HTTP 调用：

```text
Controller
  -> Application Service
  -> IHttpApiClient
  -> Infrastructure.HttpApiClient
  -> 外部系统
```

正常情况下，Controller 不应该直接调用 Repository。

## 六、新功能开发顺序

开发一个新的业务接口时，按以下顺序执行：

### 第 1 步：确认接口契约

在 `EAP.Models` 创建或修改：

```text
Models/<业务名称>/xxxRequest.cs
Models/<业务名称>/xxxResponse.cs
```

请求模型只放请求字段和基础格式校验。

### 第 2 步：定义业务接口

在 `EAP.Application/Interfaces` 创建：

```text
IxxxService.cs
IxxxRepository.cs
```

Service 接口面向 Controller，Repository 接口面向数据访问需求。

### 第 3 步：实现业务服务

在 `EAP.Application/Services` 创建服务：

```text
XxxService.cs
```

服务中处理：

- 参数和业务校验
- 调用顺序
- 是否需要事务
- 返回结果转换
- 本地化消息
- 业务日志和耗时日志

错误信息使用资源键，例如：

```csharp
throw new RequestValidationException(_localizer["InvalidRequest"]);
```

### 第 4 步：实现数据库访问

在 `EAP.DataAccess/Repositories` 创建 Repository。

如果 SQL 返回的字段需要专用映射模型，在 `EAP.DataAccess/Models/<业务名称>` 创建 `xxxRow`；Repository 内部将 `xxxRow` 映射为 Application 模型，不把数据库 Row 类型暴露到 Application 接口。

要求：

- SQL 使用参数化。
- 数据库连接使用 `await using`。
- 所有数据库 I/O 使用异步方法。
- 需要多条写操作原子完成时使用事务。
- 动态表名、字段名、排序字段必须使用白名单。
- SQL Server、Oracle、MySQL 的差异放在 Infrastructure 内部。

### 第 5 步：注册依赖

在 `EAP.DataAccess` 的依赖注入扩展中注册 Repository，在 `EAP.Infrastructure` 的依赖注入扩展中注册 JWT 和外部基础设施，在 Application 的依赖注入扩展中注册 Service。

不要在 Controller 中手动 `new` Service 或 Repository。

### 第 6 步：创建 Controller

在 `EAP.Api/Controllers` 创建轻量 Controller：

```csharp
[ApiController]
[Route("fis/Example")]
public sealed class ExampleController : ControllerBase
{
    private readonly IExampleService _service;

    public ExampleController(IExampleService service)
    {
        _service = service;
    }

    [HttpPost("GetInformation")]
    public Task<ApiResult<ExampleResponse>> GetInformation(
        [FromBody] ExampleRequest request,
        CancellationToken cancellationToken)
    {
        return _service.GetInformationAsync(request, cancellationToken);
    }
}
```

已有接口的路由不能随意修改，否则会影响调用方。新增接口的路由命名要先和团队确认。

### 第 7 步：补充资源文件

在 Contracts 的资源文件中维护中文和英文：

```text
SharedResource.resx       默认/英文
SharedResource.zh-CN.resx 中文
```

客户端通过标准请求头选择语言：

```http
Accept-Language: zh-CN
Accept-Language: en-US
```

### 第 8 步：补充测试

单元测试至少覆盖：

- 成功路径
- 参数错误
- 数据不存在
- 业务规则失败
- 数据库异常或外部接口失败
- 事务回滚
- 重试和取消操作

API 集成测试覆盖：

- Token 获取
- Token 验证
- 中英文返回
- HTTP 状态码
- 统一响应格式
- 健康检查

## 七、数据库开发规范

数据库支持：

- SQL Server
- Oracle
- MySQL

数据库连接通过配置中的逻辑名称选择，例如：

```text
Dashboard
EbsOracle
OaSqlServer
```

Repository 不直接读取配置文件，而是通过数据库连接工厂获取连接。

所有连接、命令、事务必须及时释放。SQL 参数必须使用 Dapper 参数对象，不能把客户端输入直接拼接进 SQL。

表名、字段名、排序方式无法参数化时，必须使用白名单映射。

数据库结构变更必须同时提交 `Database` 目录下的可审计 SQL 脚本。

## 八、Token、语言和统一响应

Token 获取接口当前为：

```http
POST /fis/Auth/token
```

业务接口默认需要 JWT。Token 接口和健康检查接口允许匿名访问。

所有业务接口返回统一结构：

```csharp
ApiResult<T>
```

全局异常处理中间件统一处理未捕获异常、业务异常、401 和 403，不在每个 Controller 中重复编写异常响应。

## 九、日志规范

日志由 API 入口统一输出文本日志，并按天滚动文件。

日志重点保留：

- TraceId
- RequestId
- HTTP 方法和路径
- 状态码
- 总耗时
- Controller、Application、Database 的调用链耗时
- 失败层级、错误代码和异常堆栈

示例：

```text
[REQUEST] Controller[ExampleController].GetInformation: 20.4 ms > Application[ExampleService].GetInformationAsync: 18.2 ms > Database[Dashboard].GetInformationAsync: 12.7 ms | HTTP=POST /fis/Example/GetInformation | Status=200 | Total=24.1 ms
```

禁止记录：

- JWT
- ClientSecret
- 数据库密码
- 完整连接字符串
- 敏感业务数据

## 十、命名规范

```text
接口：IExampleService
服务：ExampleService
数据库访问：ExampleRepository
控制器：ExamplesController
请求：CreateExampleRequest
响应：ExampleResponse
内部模型：ExampleModel
数据库查询结果：ExampleRow
数据库实体：ExampleEntity（只有确实存在独立持久化实体时才创建）
配置：DatabaseOptions
异步方法：GetAsync / CreateAsync
```

文件名应与主要公开类型一致。

## 十一、提交前检查

- [ ] Controller 没有 SQL 和复杂业务逻辑
- [ ] Application 没有引用 Infrastructure
- [ ] SQL 已参数化
- [ ] 连接和事务可以正常释放
- [ ] 所有 I/O 使用 async/await
- [ ] 已传递 CancellationToken
- [ ] 返回信息使用资源文件
- [ ] 已补充中文和英文资源
- [ ] 已补充单元测试
- [ ] 需要时已补充 API 集成测试
- [ ] 数据库脚本已提交
- [ ] 未记录密码、Token 和连接字符串
- [ ] `dotnet build` 成功
- [ ] `dotnet test` 全部通过

## 十二、本地验证命令

```powershell
dotnet restore EAP.WebApi.sln
dotnet build EAP.WebApi.sln -c Release --no-restore
dotnet test EAP.WebApi.sln -c Release --no-build
dotnet run --project EAP.Api/EAP.Api.csproj
```

打开 Swagger 后，先调用：

```http
POST /fis/Auth/token
```

然后在 Swagger 的授权输入框中填写：

```text
Bearer {accessToken}
```

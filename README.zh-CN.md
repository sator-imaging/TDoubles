[![nuget](https://img.shields.io/nuget/vpre/SatorImaging.TDoubles)](https://www.nuget.org/packages/SatorImaging.TDoubles)
[![build](https://github.com/sator-imaging/TDoubles/actions/workflows/build.yml/badge.svg)](https://github.com/sator-imaging/TDoubles/actions/workflows/build.yml)
&nbsp;
[![DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/sator-imaging/TDoubles)

![Hero](https://github.com/sator-imaging/TDoubles/raw/main/GitHub-SocialPreview.png)

[🇺🇸 English](./README.md)
&nbsp; ❘ &nbsp;
[🇯🇵 日本語版](./README.ja.md)
&nbsp; ❘ &nbsp;
[🇨🇳 简体中文版](./README.zh-CN.md)


`TDoubles`* 是一个强大的 C# 源生成器，通过在编译时创建模拟包装类来彻底改变单元测试。该生成器在编译期间生成清晰、可读的 C# 代码，用可定制的行为包装您的目标类型，而不是像传统模拟框架那样依赖复杂的运行时反射或代理生成。

<i>* **T** <sup>测试 / 类型安全</sup> Doubles</i>

```cs
using TDoubles;

public interface IDataService
{
    string GetData(int id);
    void SaveData(string data);
}

[Mock(typeof(IDataService))]
partial class DataServiceMock
{
    // 实现将自动生成
}
```

这里展示了如何在您的代码中使用模拟：

```cs
// 创建模拟
var mockService = new DataServiceMock();

// 覆盖测试行为
mockService.MockOverrides.GetData = (id) => $"MockData_{id}";

string mockData = mockService.GetData(123); // 返回 "MockData_123"
```

您可以委托给真实实现并覆盖模拟的部分行为。

```cs
var mock = new DataServiceMock(new ConcreteDataService());

// 使用默认行为（委托给真实服务）
var realData = mock.GetData(123);

// 覆盖部分行为进行测试
mock.MockOverrides.SaveData = (data) => Console.WriteLine($"Saved: {data}");
mock.SaveData(realData);
```

结合真实实现的最新更新，为调试目的实现虚假行为。

```cs
[Mock(typeof(IFoo), nameof(IFoo.Save), nameof(IFoo.Load))]
partial class FooFake
{
    public void Save() => File.WriteAllText("...", JsonUtility.ToJson(this, true));
    public void Load() => JsonUtility.FromJsonOverwrite(File.ReadAllText("..."), this);
}

// 委托给最新的 ConcreteFoo 实现，除了 Save 和 Load
var fake = new FooFake(new ConcreteFoo());
```

## 泛型类型支持

`TDoubles` 支持对未绑定和已关闭构造的泛型进行泛型类型模拟。

```cs
[Mock(typeof(IList<int>))]
partial class ListIntMock {} 

// TKey 的正确类型约束会自动生成，
// 并且类型参数命名不匹配也会得到解决
[Mock(typeof(IDictioanry<,>))]
partial class DictionaryMock<T, U> {}
```

## 高效回调支持

有高效的扩展点可以为每个模拟成员调用实现自定义回调。

> [!TIP]
> 根据 C# 规范，当您的模拟类声明中未实现方法体时，`partial void` 方法调用将从构建的程序集中完全移除。
> 
> https://learn.microsoft.com/zh-cn/dotnet/csharp/language-reference/keywords/partial-member

```cs
[Mock(typeof(IList<>))]
partial class ListSpy<T>  // 🕵 < 调查嫌疑人！
{
    readonly Dictionary<string, int> _callCountByName = new();

    // 不分配 object[] 实例
    partial void OnWillMockCall(string memberName)
    {
        if (!_callCountByName.TryGetValue(memberName, out var current))
        {
            current = 0;
        }
        _callCountByName[memberName] = current + 1;
    }

    // 另一个重载可以接受传递给模拟成员的参数
    // * Array.Empty<object>() 用于无参数成员
    partial void OnWillMockCall(string memberName, object?[] args)
    {
        // 如何确定方法重载
        if (memberName == "Add")
        {
            if (args[0] is T)
            {
                Console.WriteLine("Add(T item) 被调用。");
            }
            else
            {
                Console.WriteLine("Add(object item) 被调用。");
            }
        }
    }
}
```

## `Mock` 属性选项

可以选择生成的模拟成员。

```cs
// 包含内部类型、接口和成员以进行模拟生成
[Mock(typeof(Foo), IncludeInternals = true)]
partial class FooMock { }

// 从模拟生成中排除指定成员（如果未找到成员则不报错）
[Mock(typeof(Foo), "ToString", "Foo", "Bar", IncludeInternals = false)]
partial class FooMockWithoutToStringOverride
{
    // 您可以根据需要重新实现被排除的 'ToString'
    public override string ToString() => base.ToString() ?? "<NULL>";
}
```

# 介绍

该生成器通过分析带有 `[Mock]` 属性的类型，并生成相应的模拟类来工作，这些模拟类委托给原始实现，同时通过简单、强类型的 API 提供覆盖功能。这种方法消除了基于反射的模拟的性能开销，同时保持了完全的类型安全和 IntelliSense 支持。

## 主要优点

- **零运行时开销**：模拟类在编译时生成，消除了反射成本并提高了测试执行性能
- **完全类型安全**：生成的模拟提供完整的 IntelliSense 支持、编译时检查和重构安全性
- **最少设置**：只需添加 NuGet 包，将 `[Mock]` 属性应用于部分类，生成器会处理其余部分
- **通用兼容性**：支持所有主要的 C# 类型构造，包括接口、类、记录、记录结构、常规结构和静态类
- **高级泛型支持**：处理复杂的泛型场景，包括类型约束、嵌套泛型和泛型方法重载
- **内部成员访问**：可选的 `IncludeInternals` 配置允许模拟内部成员以进行全面测试
- **干净的生成代码**：生成人类可读、可调试的模拟实现，与您的代码库无缝集成

## 用例

TDoubles 生成器在以下场景中表现出色：

- **高性能测试**：当测试执行速度至关重要且反射开销不可接受时
- **遗留代码测试**：模拟未设计为接口的现有类和结构
- **静态方法测试**：通过模拟包装器将静态方法转换为可测试的实例方法
- **记录和结构模拟**：测试传统框架难以处理的值类型和不可变记录
- **复杂泛型测试**：模拟具有多个类型参数和约束的泛型类型
- **内部 API 测试**：测试内部成员而不将其公开

## 与传统模拟框架的比较

| 特性 | TDoubles | 传统框架 (Moq, NSubstitute) |
|-----------|---------------------------|-------------------------------------------|
| **性能** | 零运行时开销，编译时生成 | 运行时反射和代理创建 |
| **类型安全** | 完全编译时检查和 IntelliSense | 运行时配置，有限的 IntelliSense |
| **支持的类型** | 类、接口、记录、结构、静态类 | 主要为接口和虚成员 |
| **设置复杂性** | 单个属性，最少配置 | 复杂的流式 API 和设置表达式 |
| **调试** | 生成的代码可读且可调试 | 代理对象可能难以调试 |
| **泛型支持** | 完全支持，包括约束 | 有限的泛型类型支持 |
| **静态方法** | 转换为可测试的实例方法 | 需要包装器接口或特殊工具 |
| **值类型** | 本机支持结构和记录 | 不支持或需要装箱 |

## 工作原理

1. **标记目标类型**：将 `[Mock(typeof(TargetType))]` 属性应用于部分类
2. **编译时生成**：源生成器分析您的目标类型并创建模拟实现
3. **委托与覆盖**：生成的模拟委托给原始实例，同时为自定义行为提供 `MockOverrides`
4. **自信测试**：在您的测试中使用生成的模拟，具有完全的类型安全和性能

### 委托的伪代码

这是委托的伪代码。实际代码更复杂，因为它需要支持 `ref` 和 `out` 参数修饰符。

```cs
public string GetData(int id)
{
    // 如果是值类型或可空引用类型则返回 'default'，否则抛出异常
    return MockOverrides.GetData?.Invoke(id)
        ?? _target?.GetData(id)
        ?? throw new TDoublesException(...);
}
```

### 生成的模拟结构

当您创建模拟类时，生成器会添加几个成员：

```cs
[Mock(typeof(IUserService))]
partial class UserServiceMock
{
    // 由源生成器生成：

    // 接受目标实例的构造函数
    public UserServiceMock(IUserService? target = default) { }

    // 访问底层目标
    public IUserService? MockTarget { get; }

    // 统一回调
    partial void OnWillMockCall(string memberName);
    partial void OnWillMockCall(string memberName, object?[] args);

    // 覆盖配置对象
    public sealed class MockOverrideContainer { }
    public MockOverrideContainer MockOverrides { get; }

    // 所有接口/类成员都已实现
    public string GetUserName(int userId) { /* 生成的实现 */ }
    public Task<bool> DeleteUser(int userId) { /* 生成的实现 */ }
    // ... 等等
}
```

# 安装

## NuGet 包安装

### 包管理器控制台

```powershell
Install-Package SatorImaging.TDoubles
```

### .NET CLI

```bash
dotnet add package SatorImaging.TDoubles
```

### PackageReference (手动)

将以下内容添加到您的项目文件 (`.csproj`)：

```xml
<PackageReference Include="SatorImaging.TDoubles" Version="1.0.0" />
```

## 系统要求

- **.NET Framework**：.NET Standard 2.0 或更高版本
- **C# 语言版本**：C# 7.3 或更高版本
- **兼容运行时**：
    - .NET Framework 4.6.1+
    - .NET Core 2.0+
    - .NET 5.0+
    - Unity 2022.3.12f1 或更高版本

## 设置和配置

### 基本设置

1. 使用上述方法之一安装 NuGet 包
2. 重建您的项目以启用源生成器
3. 使用 `[Mock]` 属性创建部分类以生成模拟

### 项目配置

无需额外的项目配置。当包安装后，源生成器会自动激活，并在编译期间生成模拟类。

### 验证

要验证安装是否成功：

1. 向您的项目添加一个简单的模拟类：
   ```cs
   using TDoubles;
   
   public interface ITestService
   {
       string GetMessage();
   }
   
   [Mock(typeof(ITestService))]
   partial class TestServiceMock
   {
       // 模拟实现将在此处生成
   }
   ```
2. 构建您的项目
3. 检查是否没有发生编译错误，并且模拟类已生成

### IDE 支持
- **Visual Studio**：对生成的模拟类提供完整的 IntelliSense 支持
- **Visual Studio Code**：与 C# 扩展配合使用
- **JetBrains Rider**：完全支持代码补全
- **命令行**：与 `dotnet build` 和 `msbuild` 配合使用

# 基本用法

本节提供分步示例，帮助您开始使用 `TDoubles`。所有示例都是完整的，可以直接在您的项目中使用。

## 先决条件

在使用 TDoubles 生成器之前，请确保您的模拟类满足以下要求：

1. **部分类**：模拟类必须声明为 `partial`
2. **Mock 属性**：将 `[Mock(typeof(TargetType))]` 应用于部分类
3. **命名空间**：包含 `using TDoubles;`
4. **可见性**：可以使用任何可见性修饰符（public、internal 等）- 生成的成员将继承相同的可见性

## 简单接口模拟

最常见的场景是模拟接口以进行依赖注入测试。

### 示例：用户服务接口

```cs
using TDoubles;
using System;
using System.Threading.Tasks;

// 定义您的接口
public interface IUserService
{
    string GetUserName(int userId);
    Task<bool> DeleteUser(int userId);
    bool IsUserActive(int userId);
}

// 创建一个部分模拟类
[Mock(typeof(IUserService))]
partial class UserServiceMock
{
    // 源生成器将在此处创建完整的实现
}

// 在测试中的使用示例
class Program
{
    static void Main()
    {
        // 创建一个具体实现用于委托
        var realService = new ConcreteUserService();
        
        // 创建模拟，以真实服务作为底层目标
        var mockService = new UserServiceMock(realService);
        
        Console.WriteLine("=== 默认行为（委托给真实服务） ===");
        Console.WriteLine($"用户名: {mockService.GetUserName(123)}");
        Console.WriteLine($"是否活跃: {mockService.IsUserActive(123)}");
        
        Console.WriteLine("\n=== 带有覆盖的自定义行为 ===");
        
        // 覆盖特定方法进行测试
        mockService.MockOverrides.GetUserName = (userId) => $"MockUser_{userId}";
        mockService.MockOverrides.IsUserActive = (userId) => userId > 100;
        
        Console.WriteLine($"用户名（已覆盖）: {mockService.GetUserName(123)}");
        Console.WriteLine($"是否活跃（已覆盖）: {mockService.IsUserActive(50)}");
        Console.WriteLine($"是否活跃（已覆盖）: {mockService.IsUserActive(150)}");
        
        // 如果需要，访问底层真实服务
        Console.WriteLine($"真实服务: {mockService.MockTarget.GetUserName(123)}");
    }
}

// 演示用的具体实现
public class ConcreteUserService : IUserService
{
    public string GetUserName(int userId) => $"RealUser_{userId}";
    public async Task<bool> DeleteUser(int userId) => await Task.FromResult(true);
    public bool IsUserActive(int userId) => true;
}
```

## 带有继承的类模拟

模拟具体类以测试继承场景和虚方法覆盖。

### 示例：带有虚方法的服务类

```cs
using TDoubles;
using System;

// 带有虚方法的基服务类
public class DatabaseService
{
    public virtual string GetConnectionString() => "Server=localhost;Database=prod;";
    public virtual void SaveData(string data) => Console.WriteLine($"Saving to database: {data}");
    public virtual int GetRecordCount() => 1000;
    
    // 非虚方法（将被包装但不可覆盖）
    public string GetServiceName() => "DatabaseService";
}
// 为类创建模拟
[Mock(typeof(DatabaseService))]
partial class DatabaseServiceMock
{
    // 生成的实现将包装所有公共方法
}

// 使用示例
class Program
{
    static void Main()
    {
        // 创建真实服务实例
        var realService = new DatabaseService();
        
        // 创建模拟包装器
        var mockService = new DatabaseServiceMock(realService);
        
        Console.WriteLine("=== 默认行为 ===");
        Console.WriteLine($"连接: {mockService.GetConnectionString()}");
        Console.WriteLine($"服务名称: {mockService.GetServiceName()}");
        Console.WriteLine($"记录计数: {mockService.GetRecordCount()}");
        mockService.SaveData("测试数据");
        
        Console.WriteLine("\n=== 测试场景覆盖 ===");
        
        // 覆盖测试场景
        mockService.MockOverrides.GetConnectionString = () => "Server=testserver;Database=test;";
        mockService.MockOverrides.GetRecordCount = () => 0; // 模拟空数据库
        mockService.MockOverrides.SaveData = (data) => Console.WriteLine($"测试模式: 将保存 '{data}'");
        
        Console.WriteLine($"测试连接: {mockService.GetConnectionString()}");
        Console.WriteLine($"测试记录计数: {mockService.GetRecordCount()}");
        mockService.SaveData("测试数据");
        
        // 非虚方法仍然有效，但委托给原始方法
        Console.WriteLine($"服务名称（始终委托）: {mockService.GetServiceName()}");
    }
}
```

## 继承和接口实现

模拟同时继承自基类并实现接口的类。

### 示例：复杂服务层次结构

```cs
using TDoubles;
using System;

// 接口定义
public interface INotificationService
{
    void SendNotification(string message);
    bool IsServiceAvailable();
}

// 带有虚方法的基类
public class BaseService
{
    public virtual string GetServiceType() => "Base";
    public virtual void Initialize() => Console.WriteLine("基本初始化");
}

// 继承并实现接口的具体类
public class EmailService : BaseService, INotificationService
{
    public override string GetServiceType() => "Email";
    public override void Initialize() => Console.WriteLine("电子邮件服务已初始化");
    
    public void SendNotification(string message) => Console.WriteLine($"电子邮件: {message}");
    public bool IsServiceAvailable() => true;
}

// 模拟具体类
[Mock(typeof(EmailService))]
partial class EmailServiceMock
{
    // 模拟继承的方法和接口实现
}

// 使用示例
class Program
{
    static void Main()
    {
        var realService = new EmailService();
        var mockService = new EmailServiceMock(realService);
        
        Console.WriteLine("=== 测试继承方法 ===");
        Console.WriteLine($"服务类型: {mockService.GetServiceType()}");
        mockService.Initialize();
        
        Console.WriteLine("\n=== 测试接口方法 ===");
        mockService.SendNotification("你好世界");
        Console.WriteLine($"可用: {mockService.IsServiceAvailable()}");
        
        Console.WriteLine("\n=== 带有覆盖的测试 ===");
        
        // 覆盖继承方法
        mockService.MockOverrides.GetServiceType = () => "MockEmail";
        mockService.MockOverrides.Initialize = () => Console.WriteLine("模拟初始化");
        
        // 覆盖接口方法
        mockService.MockOverrides.SendNotification = (msg) => Console.WriteLine($"模拟电子邮件: {msg}");
        mockService.MockOverrides.IsServiceAvailable = () => false;
        
        Console.WriteLine($"服务类型: {mockService.GetServiceType()}");
        mockService.Initialize();
        mockService.SendNotification("测试消息");
        Console.WriteLine($"可用: {mockService.IsServiceAvailable()}");
    }
}
```

# 高级用法

有关包括泛型类型、静态类、记录、结构和内部成员访问在内的高级场景，请参阅 [高级用法指南](docs/advanced-usage.md)。

# 测试示例

有关 MSTest、NUnit 和性能比较的全面测试示例，请参阅 [测试示例指南](docs/testing-examples.md)。

# 技术说明

## `record` 和 `record struct`

- 始终实现 `IEquatable<MOCK_TARGET_RECORD>` 和 `MockOverrides.MockTargetRecord_Equals`
    - 请注意，它 *不是* `IEquatable<GENERATED_MOCK>`
- `bool Equals(object?)` 无法被覆盖

# 已知限制和不支持的场景

## 泛型方法的类型参数

当方法使用方法级类型参数而不是类型级参数时，`MockOverrides` 将使用 `object` 而不是方法级类型参数。

```cs
// 生成的模拟具有类型级参数 T
partial class Mock<T>
{
    // 生成的模拟方法具有 T 和 TMethod 类型参数
    public TMethod GenericMethod<T, TMethod>(T input) { ... }

    // <TMethod> 可以添加到此类中，但它也必须作为类型级参数公开...
    public sealed class MockOverrideContainer
    {
        // 使用了类型级参数 T，但 TMethod 被遮蔽为 object
        public Func<T, object> GenericMethod { get; set; }
        //             ~~~~~~ 不是 TMethod
    }
}
```

> [!NOTE]
> 生成的模拟方法返回 `TMethod`，就像模拟目标一样。在内部，模拟方法在返回值时会将覆盖的 `object` 结果转换为 `TMethod`。

## 类型系统限制

**不支持的类型：**
- 枚举（请改用包装类）
- 委托和函数指针
- 基本类型（`int`、`string` 等）
- 仅包含静态构造函数的静态类
- 需要实现的纯虚方法的抽象类
- `object`、`ValueType`、`Enum` 和其他特殊类型，例如 `Span<T>`

## 类型约束限制

**不支持的约束：**
- `where T : default`
- `where T : allows ref struct`

## 返回类型限制

**不支持的类型：**
- `ref` 返回类型

## 属性限制

类型、方法、属性等上的属性在生成的模拟中不保留。

## 方法和属性限制

**不支持的成员：**
- 在某些复杂场景中的 `ref` 和 `out` 参数 (?)
- ~~带有 `__arglist`（可变参数）的方法~~ 
- ~~带有名称冲突的显式接口实现~~ 
- ~~具有复杂 getter/setter 可访问性组合的属性~~ 

**部分支持：**

```cs
public interface IService
{
    // ✅ 完全支持
    string GetData(int id);
    Task<bool> ProcessAsync(string data);
    
    // ⚠️ 有限支持 - 可能无法正确覆盖
    ref int GetReference();
    void ProcessData(__arglist);
}
```

## 泛型方法限制

某些有效的类型约束未正确转换。我们目前没有计划支持这种边缘情况的类型约束。

> 注意：`override` 方法不能有类型约束，除了 `class` 和 `struct`。

```cs
// 抽象方法声明，返回 (M, N?)，带有 where M : N? 约束
public abstract (M t, N? u) TypeArgMappingNullable_Abstract<M, N>() where M : N?;

// 预期（有效）返回类型是 (M, N)
public override (M t, N u) TypeArgMappingNullable_Abstract<M, N>() { }

// 但得到 (M, N?)
public override (M t, N? u) TypeArgMappingNullable_Abstract<M, N>() { }
```

## 继承和接口限制

**多接口实现：**
- ~~支持，但显式接口实现可能存在命名冲突~~ 
- 菱形继承模式可能导致方法解析问题

**虚方法覆盖：**
- ~~只有 `virtual` 和 `abstract` 方法可以在类模拟中被覆盖~~ 
- `sealed` 方法不能被覆盖（将委托给原始方法）

## 平台和框架限制

**框架支持：**
- 需要 .NET Standard 2.0 或更高版本
- 源生成器需要 C# 7.3 或更高版本
- 某些高级 C# 11+ 功能可能未完全支持

**IDE 集成：**
- 新生成的模拟的 IntelliSense 可能会延迟
- 某些 IDE 可能需要重建才能识别生成的代码
- 调试生成的代码可能会显示优化/合成代码

# 贡献

我们欢迎并感谢社区的贡献！无论是修复错误、添加功能、改进文档还是提供反馈，您的贡献都有助于使 TDoubles 对每个人都更好。

请参阅 [CONTRIBUTING.md](CONTRIBUTING.md)

# 行为准则

我们致力于为所有贡献者提供一个热情和包容的环境。请在所有互动中保持尊重和专业。

# 支持和社区

## 获取帮助

如果您遇到本故障排除指南中未涵盖的问题：

1. **检查 GitHub Issues**：搜索现有问题以查找类似问题
2. **创建最小复现**：提供演示问题的最小代码示例
3. **包含构建输出**：分享相关的编译器错误和警告
4. **指定环境**：包括 .NET 版本、IDE 和操作系统详细信息

**支持渠道：**
- [GitHub Discussions](https://github.com/sator-imaging/TDoubles/discussions) - 问题和社区支持
- [GitHub Issues](https://github.com/sator-imaging/TDoubles/issues) - 错误报告和功能请求

## 报告安全问题

如果您发现安全漏洞，请通过电子邮件私下报告给维护者，而不是创建公开问题。这使我们能够在漏洞广为人知之前解决它。

# 项目信息

## 待办事项：寻求帮助

- 缺失测试
    - `static` 类模拟
    - `sealed` 覆盖方法
    - `async` 测试
    - `event` getter 和 setter 测试
    - `readonly struct` 测试
    - `readonly record struct` 测试
    - `Tuple` 和 `ValueTuple` 测试
    - 属性和索引器可访问性测试（例如，`{ get; private set; }` 或等等）
- 缺失功能
    - `ref` 返回
    - 属性保留
    - 支持嵌套类型（例如，`[Mock(typeof(Foo.Bar))]`）
    - 嵌套泛型类型模拟（例如，`[Mock(typeof(Foo.NestedKeyValueStore<,>))]`）
    - 为模拟成员添加适当的 `<inheritdoc cref="..." />`
    - 支持 `default` 和 `allows ref struct` 类型约束
        - `default` 约束仅在覆盖和显式接口实现方法上有效
        - 需要 Roslyn 更新，同时保持 Unity 引擎支持
    - 在类型参数上发出诊断错误
- 优化
    - 尽可能使用 `ImmutableArray<T>` 或 `ImmutableList<T>`
    - 消除低效的 `StringBuilder` 使用
- 重构
    - 消除 FP 编程技术
        - 将数据模型转换为领域模型以封装信息和行为
        - 将蓝图到 C# 的转换集中在领域模型中，使其保持一致、健壮和可维护
        - 消除代码库中分散的重复函数、控制流等
- 可选
    - ~~新的 `Mock` 属性选项，用于生成 `MockCallCounts`，记录每个模拟成员的调用次数~~ 
        - 声明 `volatile int` 字段
        - 在生成的模拟类成员的开头通过 `Interlocked.Increment(ref ...)` 方法增加计数。

## 作者和维护者

**Sator Imaging**
- GitHub: [@sator-imaging](https://github.com/sator-imaging)
- 项目仓库: [sator-imaging/TDoubles](https://github.com/sator-imaging/TDoubles)

## 致谢

我们感谢所有通过代码贡献、错误报告、功能建议和社区支持帮助改进此项目的贡献者。

## 许可证

本项目根据 **MIT 许可证** 获得许可。

### 第三方许可证

本项目使用以下第三方包：
- **Microsoft.CodeAnalysis.CSharp** (MIT 许可证)
- **Microsoft.CodeAnalysis.Analyzers** (MIT 许可证)

---

**&copy; 2025 Sator Imaging. 保留所有权利。**

如需支持、问题或贡献，请访问我们的 [GitHub 仓库](https://github.com/sator-imaging/TDoubles)。

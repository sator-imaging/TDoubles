[![nuget](https://img.shields.io/nuget/vpre/SatorImaging.TDoubles)](https://www.nuget.org/packages/SatorImaging.TDoubles)
[![build](https://github.com/sator-imaging/TDoubles/actions/workflows/build.yml/badge.svg)](https://github.com/sator-imaging/TDoubles/actions/workflows/build.yml)
&nbsp;
[![DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/sator-imaging/TDoubles)

[🇺🇸 English](./README.md)
&nbsp; ❘ &nbsp;
[🇯🇵 日本語版](./README.ja.md)
&nbsp; ❘ &nbsp;
[🇨🇳 简体中文版](./README.zh-CN.md)


![Hero](https://github.com/sator-imaging/TDoubles/raw/main/GitHub-SocialPreview.png)

`TDoubles`* は、コンパイル時にモックラッパークラスを作成することで単体テストに革命をもたらす強力な C# ソースジェネレーターです。従来のモックフレームワークのように複雑なランタイムリフレクションやプロキシ生成に依存するのではなく、このジェネレーターはコンパイル中にクリーンで読みやすい C# コードを生成し、カスタマイズ可能な動作でターゲット型をラップします。

<i>* **T** <sup>Test / Type-Safety</sup> Doubles</i>

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
    // 実装は自動的に生成されます
}
```

以下に、コードでモックを使用する方法を示します。

```cs
// モックを作成
var mockService = new DataServiceMock();

// テストのために動作をオーバーライド
mockService.MockOverrides.GetData = (id) => $"MockData_{id}";

string mockData = mockService.GetData(123); // "MockData_123" を返します
```

実際の実装に委譲し、モックの部分的な動作をオーバーライドできます。

```cs
var mock = new DataServiceMock(new ConcreteDataService());

// デフォルトの動作を使用 (実際のサービスに委譲)
var realData = mock.GetData(123);

// テストのために部分的な動作をオーバーライド
mock.MockOverrides.SaveData = (data) => Console.WriteLine($"Saved: {data}");
mock.SaveData(realData);
```

最新の実際の実装と連携して、デバッグ目的でフェイク動作を実装します。

```cs
[Mock(typeof(IFoo), nameof(IFoo.Save), nameof(IFoo.Load))]
partial class FooFake
{
    public void Save() => File.WriteAllText("...", JsonUtility.ToJson(this, true));
    public void Load() => JsonUtility.FromJsonOverwrite(File.ReadAllText("..."), this);
}

// Save と Load を除く最新の ConcreteFoo 実装に委譲
var fake = new FooFake(new ConcreteFoo());
```

## ジェネリック型サポート

`TDoubles` は、アンバウンドおよびクローズドコンストラクトジェネリックの両方でジェネリック型モックをサポートします。

```cs
[Mock(typeof(IList<int>))]
partial class ListIntMock {}

// TKey の適切な型制約が自動的に生成され、
// 型パラメーター名の不一致も解決されます
[Mock(typeof(IDictioanry<,>))]
partial class DictionaryMock<T, U> {}
```

## 効率的なコールバックサポート

各モックメンバー呼び出しに対してカスタムコールバックを実装するための効率的な拡張ポイントがあります。

> [!TIP]
> C# の仕様として、`partial void` メソッド呼び出しは、モッククラス宣言でメソッド本体が実装されていない場合、ビルドされたアセンブリから完全に削除されます。
> 
> https://learn.microsoft.com/ja-jp/dotnet/csharp/language-reference/keywords/partial-member

```cs
[Mock(typeof(IList<>))]
partial class ListSpy<T>  // 🕵 < 容疑者を調査！
{
    readonly Dictionary<string, int> _callCountByName = new();

    // object?[] インスタンスを割り当てずに
    partial void OnWillMockCall(string memberName)
    {
        if (!_callCountByName.TryGetValue(memberName, out var current))
        {
            current = 0;
        }
        _callCountByName[memberName] = current + 1;
    }

    // 別のオーバーロードは、モックメンバーに渡された引数を受け取ることができます
    // * パラメーターなしのメンバーには Array.Empty<object>() が使用されます
    partial void OnWillMockCall(string memberName, object?[] args)
    {
        // メソッドのオーバーロードを決定する方法
        if (memberName == "Add")
        {
            if (args[0] is T)
            {
                Console.WriteLine("Add(T item) が呼び出されました。");
            }
            else
            {
                Console.WriteLine("Add(object item) が呼び出されました。");
            }
        }
    }
}
```

## `Mock` 属性オプション

生成されるモックメンバーを選択するオプションがあります。

```cs
// 内部型、インターフェース、メンバーをモック生成に含める
[Mock(typeof(Foo), IncludeInternals = true)]
partial class FooMock { }

// 指定されたメンバーをモック生成から除外 (メンバーが見つからなくてもエラーなし)
[Mock(typeof(Foo), "ToString", "Foo", "Bar", IncludeInternals = false)]
partial class FooMockWithoutToStringOverride
{
    // 除外された 'ToString' を必要に応じて再実装できます
    public override string ToString() => base.ToString() ?? "<NULL>";
}
```

# はじめに

このジェネレーターは、`[Mock]` 属性でマークされた型を分析し、元の実装に委譲しながら、シンプルで厳密に型付けされた API を介してオーバーライド機能を提供する対応するモッククラスを生成することで機能します。このアプローチにより、リフレクションベースのモックのパフォーマンスオーバーヘッドが排除され、完全な型安全性と IntelliSense サポートが維持されます。

## 主な利点

- **ゼロランタイムオーバーヘッド**: モッククラスはコンパイル時に生成されるため、リフレクションコストが排除され、テスト実行パフォーマンスが向上します。
- **完全な型安全性**: 生成されたモックは、完全な IntelliSense サポート、コンパイル時チェック、リファクタリングの安全性を備えています。
- **最小限のセットアップ**: NuGet パッケージを追加し、`[Mock]` 属性を部分クラスに適用するだけで、残りはジェネレーターが処理します。
- **普遍的な互換性**: インターフェース、クラス、レコード、レコード構造体、通常の構造体、静的クラスを含むすべての主要な C# 型コンストラクトをサポートします。
- **高度なジェネリックサポート**: 型制約、ネストされたジェネリック、ジェネリックメソッドのオーバーロードを含む複雑なジェネリックシナリオを処理します。
- **内部メンバーアクセス**: オプションの `IncludeInternals` 設定により、包括的なテストのために内部メンバーのモックが可能です。
- **クリーンな生成コード**: 人間が読みやすく、デバッグ可能なモック実装を生成し、コードベースとシームレスに統合します。

## ユースケース

TDoubles ジェネレーターは、次のようなシナリオで優れています。

- **高性能テスト**: テスト実行速度が重要で、リフレクションオーバーヘッドが許容できない場合。
- **レガシーコードテスト**: インターフェースで設計されていない既存のクラスや構造体をモックする場合。
- **静的メソッドテスト**: モックラッパーを介して静的メソッドをテスト可能なインスタンスメソッドに変換する場合。
- **レコードと構造体のモック**: 従来のフレームワークでは困難な値型と不変レコードをテストする場合。
- **複雑なジェネリックテスト**: 複数の型パラメーターと制約を持つジェネリック型をモックする場合。
- **内部 API テスト**: 内部メンバーを公開せずにテストする場合。

## 従来のモックフレームワークとの比較

| 機能 | TDoubles | 従来のフレームワーク (Moq, NSubstitute) |
|-----------|---------------------------|-------------------------------------------|
| **パフォーマンス** | ゼロランタイムオーバーヘッド、コンパイル時生成 | ランタイムリフレクションとプロキシ作成 |
| **型安全性** | 完全なコンパイル時チェックと IntelliSense | ランタイム構成、限定的な IntelliSense |
| **サポートされる型** | クラス、インターフェース、レコード、構造体、静的クラス | 主にインターフェースと仮想メンバー |
| **セットアップの複雑さ** | 単一属性、最小限の構成 | 複雑な流れるような API とセットアップ式 |
| **デバッグ** | 生成されたコードは読みやすくデバッグ可能 | プロキシオブジェクトはデバッグが困難な場合がある |
| **ジェネリックサポート** | 制約を含む完全なサポート | 限定的なジェネリック型サポート |
| **静的メソッド** | テスト可能なインスタンスメソッドに変換 | ラッパーインターフェースまたは特殊なツールが必要 |
| **値型** | 構造体とレコードのネイティブサポート | サポートされていないか、ボクシングが必要 |

## 仕組み

1. **ターゲット型をマーク**: `[Mock(typeof(TargetType))]` 属性を部分クラスに適用します。
2. **コンパイル時生成**: ソースジェネレーターがターゲット型を分析し、モック実装を作成します。
3. **オーバーライドによる委譲**: 生成されたモックは、カスタム動作のために `MockOverrides` を提供しながら、元のインスタンスに委譲します。
4. **自信を持ってテスト**: 完全な型安全性とパフォーマンスで、生成されたモックをテストで使用します。

### 委譲ロジック

以下は委譲の擬似コードです。実際のコードは、`ref` および `out` パラメーター修飾子をサポートする必要があるため、より複雑です。

```cs
public string GetData(int id)
{
    // 値型または null 許容参照型の場合は 'default' を返し、それ以外の場合はスローします
    return MockOverrides.GetData?.Invoke(id)
        ?? _target?.GetData(id)
        ?? throw new TDoublesException(...);
}
```

### 生成されたモック構造

モッククラスを作成すると、ジェネレーターはいくつかのメンバーを追加します。

```csharp
[Mock(typeof(IUserService))]
partial class UserServiceMock
{
    // ソースジェネレーターによって生成されます:

    // ターゲットインスタンスを受け取るコンストラクター
    public UserServiceMock(IUserService? target = default) { }

    // 基になるターゲットへのアクセス
    public IUserService? MockTarget { get; }

    // 統合コールバック
    partial void OnWillMockCall(string memberName);
    partial void OnWillMockCall(string memberName, object?[] args);

    // オーバーライド構成オブジェクト
    public sealed class MockOverrideContainer { }
    public MockOverrideContainer MockOverrides { get; }

    // すべてのインターフェース/クラスメンバーが実装されます
    public string GetUserName(int userId) { /* 生成された実装 */ }
    public Task<bool> DeleteUser(int userId) { /* 生成された実装 */ }
    // ... など
}
```

# インストール

## NuGet パッケージのインストール

### パッケージマネージャーコンソール

```powershell
Install-Package SatorImaging.TDoubles
```

### .NET CLI

```bash
dotnet add package SatorImaging.TDoubles
```

### PackageReference (手動)

プロジェクトファイル (`.csproj`) に以下を追加します。

```xml
<PackageReference Include="SatorImaging.TDoubles" Version="1.0.0" />
```

## システム要件

- **.NET Framework**: .NET Standard 2.0 以降
- **C# 言語バージョン**: C# 7.3 以降
- **互換性のあるランタイム**:
    - .NET Framework 4.6.1+
    - .NET Core 2.0+
    - .NET 5.0+
    - Unity 2022.3.12f1 以降

## セットアップと構成

### 基本的なセットアップ

1. 上記のいずれかの方法で NuGet パッケージをインストールします。
2. ソースジェネレーターを有効にするためにプロジェクトをリビルドします。
3. `[Mock]` 属性を持つ部分クラスを作成してモックを生成します。

### プロジェクト構成

追加のプロジェクト構成は不要です。パッケージがインストールされると、ソースジェネレーターは自動的にアクティブになり、コンパイル中にモッククラスを生成します。

### 検証

インストールが成功したことを確認するには:

1. プロジェクトにシンプルなモッククラスを追加します。
   ```csharp
   using TDoubles;
   
   public interface ITestService
   {
       string GetMessage();
   }
   
   [Mock(typeof(ITestService))]
   partial class TestServiceMock
   {
       // モック実装はここに生成されます
   }
   ```
2. プロジェクトをビルドします。
3. コンパイルエラーが発生せず、モッククラスが生成されていることを確認します。

### IDE サポート
- **Visual Studio**: 生成されたモッククラスの完全な IntelliSense サポート
- **Visual Studio Code**: C# 拡張機能で動作
- **JetBrains Rider**: コード補完を含む完全なサポート
- **コマンドライン**: `dotnet build` および `msbuild` で動作

# 基本的な使用法

このセクションでは、`TDoubles` の使用を開始するためのステップバイステップの例を示します。すべての例は完全であり、プロジェクトですぐに使用できます。

## 前提条件

TDoubles ジェネレーターを使用する前に、モッククラスが次の要件を満たしていることを確認してください。

1. **部分クラス**: モッククラスは `partial` として宣言する必要があります。
2. **Mock 属性**: `[Mock(typeof(TargetType))]` を部分クラスに適用します。
3. **名前空間**: `using TDoubles;` を含めます。
4. **可視性**: 任意の可視性修飾子 (public, internal など) を使用できます。生成されたメンバーは同じ可視性を継承します。

## シンプルなインターフェースモック

最も一般的なシナリオは、依存性注入テストのためのインターフェースのモックです。

### 例: ユーザーサービスインターフェース

```csharp
using TDoubles;
using System;
using System.Threading.Tasks;

// インターフェースを定義
public interface IUserService
{
    string GetUserName(int userId);
    Task<bool> DeleteUser(int userId);
    bool IsUserActive(int userId);
}

// 部分モッククラスを作成
[Mock(typeof(IUserService))]
partial class UserServiceMock
{
    // ソースジェネレーターがここに完全な実装を作成します
}

// テストでの使用例
class Program
{
    static void Main()
    {
        // 委譲のための具体的な実装を作成
        var realService = new ConcreteUserService();
        
        // 実際のサービスを基になるターゲットとしてモックを作成
        var mockService = new UserServiceMock(realService);
        
        Console.WriteLine("=== デフォルトの動作 (実際のサービスに委譲) ===");
        Console.WriteLine($"ユーザー名: {mockService.GetUserName(123)}");
        Console.WriteLine($"アクティブ: {mockService.IsUserActive(123)}");
        
        Console.WriteLine("\n=== オーバーライドによるカスタム動作 ===");
        
        // テストのために特定のメソッドをオーバーライド
        mockService.MockOverrides.GetUserName = (userId) => $"MockUser_{userId}";
        mockService.MockOverrides.IsUserActive = (userId) => userId > 100;
        
        Console.WriteLine($"ユーザー名 (オーバーライド): {mockService.GetUserName(123)}");
        Console.WriteLine($"アクティブ (オーバーライド): {mockService.IsUserActive(50)}");
        Console.WriteLine($"アクティブ (オーバーライド): {mockService.IsUserActive(150)}");
        
        // 必要に応じて基になる実際のサービスにアクセス
        Console.WriteLine($"実際のサービス: {mockService.MockTarget.GetUserName(123)}");
    }
}

// デモンストレーションのための具体的な実装
public class ConcreteUserService : IUserService
{
    public string GetUserName(int userId) => $"RealUser_{userId}";
    public async Task<bool> DeleteUser(int userId) => await Task.FromResult(true);
    public bool IsUserActive(int userId) => true;
}
```

## 継承によるクラスモック

継承シナリオと仮想メソッドのオーバーライドをテストするために、具象クラスをモックします。

### 例: 仮想メソッドを持つサービスクラス

```csharp
using TDoubles;
using System;

// 仮想メソッドを持つ基本サービスクラス
public class DatabaseService
{
    public virtual string GetConnectionString() => "Server=localhost;Database=prod;";
    public virtual void SaveData(string data) => Console.WriteLine($"Saving to database: {data}");
    public virtual int GetRecordCount() => 1000;
    
    // 非仮想メソッド (ラップされるがオーバーライド不可)
    public string GetServiceName() => "DatabaseService";
}

// クラスのモックを作成
[Mock(typeof(DatabaseService))]
partial class DatabaseServiceMock
{
    // 生成された実装はすべてのパブリックメソッドをラップします
}

// 使用例
class Program
{
    static void Main()
    {
        // 実際のサービスインスタンスを作成
        var realService = new DatabaseService();
        
        // モックラッパーを作成
        var mockService = new DatabaseServiceMock(realService);
        
        Console.WriteLine("=== デフォルトの動作 ===");
        Console.WriteLine($"接続: {mockService.GetConnectionString()}");
        Console.WriteLine($"サービス名: {mockService.GetServiceName()}");
        Console.WriteLine($"レコード数: {mockService.GetRecordCount()}");
        mockService.SaveData("test data");
        
        Console.WriteLine("\n=== テストシナリオのオーバーライド ===");
        
        // テストシナリオのためにオーバーライド
        mockService.MockOverrides.GetConnectionString = () => "Server=testserver;Database=test;";
        mockService.MockOverrides.GetRecordCount = () => 0; // 空のデータベースをシミュレート
        mockService.MockOverrides.SaveData = (data) => Console.WriteLine($"テストモード: '{data}' を保存します");
        
        Console.WriteLine($"テスト接続: {mockService.GetConnectionString()}");
        Console.WriteLine($"テストレコード数: {mockService.GetRecordCount()}");
        mockService.SaveData("test data");
        
        // 非仮想メソッドは引き続き機能しますが、元のものに委譲します
        Console.WriteLine($"サービス名 (常に委譲): {mockService.GetServiceName()}");
    }
}
```

## 継承とインターフェースの実装

基本クラスから継承し、インターフェースを実装するモッククラス。

### 例: 複雑なサービス階層

```csharp
using TDoubles;
using System;

// インターフェース定義
public interface INotificationService
{
    void SendNotification(string message);
    bool IsServiceAvailable();
}

// 仮想メソッドを持つ基本クラス
public class BaseService
{
    public virtual string GetServiceType() => "Base";
    public virtual void Initialize() => Console.WriteLine("基本初期化");
}

// 継承し、インターフェースを実装する具象クラス
public class EmailService : BaseService, INotificationService
{
    public override string GetServiceType() => "Email";
    public override void Initialize() => Console.WriteLine("メールサービス初期化");
    
    public void SendNotification(string message) => Console.WriteLine($"メール: {message}");
    public bool IsServiceAvailable() => true;
}

// 具象クラスをモック
[Mock(typeof(EmailService))]
partial class EmailServiceMock
{
    // 継承されたメソッドとインターフェースの実装の両方をモックします
}

// 使用例
class Program
{
    static void Main()
    {
        var realService = new EmailService();
        var mockService = new EmailServiceMock(realService);
        
        Console.WriteLine("=== 継承されたメソッドのテスト ===");
        Console.WriteLine($"サービスタイプ: {mockService.GetServiceType()}");
        mockService.Initialize();
        
        Console.WriteLine("\n=== インターフェースメソッドのテスト ===");
        mockService.SendNotification("Hello World");
        Console.WriteLine($"利用可能: {mockService.IsServiceAvailable()}");
        
        Console.WriteLine("\n=== オーバーライドによるテスト ===");
        
        // 継承されたメソッドをオーバーライド
        mockService.MockOverrides.GetServiceType = () => "MockEmail";
        mockService.MockOverrides.Initialize = () => Console.WriteLine("モック初期化");
        
        // インターフェースメソッドをオーバーライド
        mockService.MockOverrides.SendNotification = (msg) => Console.WriteLine($"モックメール: {msg}");
        mockService.MockOverrides.IsServiceAvailable = () => false;
        
        Console.WriteLine($"サービスタイプ: {mockService.GetServiceType()}");
        mockService.Initialize();
        mockService.SendNotification("テストメッセージ");
        Console.WriteLine($"利用可能: {mockService.IsServiceAvailable()}");
    }
}
```

# 高度な使用法

ジェネリック型、静的クラス、レコード、構造体、内部メンバーアクセスを含む高度なシナリオについては、[高度な使用法ガイド](docs/advanced-usage.md) を参照してください。

# テスト例

MSTest、NUnit を使用した包括的なテスト例とパフォーマンス比較については、[テスト例ガイド](docs/testing-examples.md) を参照してください。

# 技術ノート

## `record` および `record struct`

- 常に `IEquatable<MOCK_TARGET_RECORD>` と `MockOverrides.MockTargetRecord_Equals` を実装します。
    - これは `IEquatable<GENERATED_MOCK>` *ではない* ことに注意してください。
- `bool Equals(object?)` はオーバーライドできません。

# 既知の制限事項とサポートされていないシナリオ

## ジェネリックメソッドの型パラメーター

メソッドが型レベルのパラメーターではなくメソッドレベルの型パラメーターを使用する場合、`MockOverrides` はメソッドレベルの型パラメーターの代わりに `object` を使用します。

```cs
// 生成されたモックは型レベルのパラメーター T を持ちます
partial class Mock<T>
{
    // T と TMethod 型パラメーターを持つ生成されたモックメソッド
    public TMethod GenericMethod<T, TMethod>(T input) { ... }

    // <TMethod> はこのクラスに追加できますが、型レベルのパラメーターとしても公開する必要があります...
    public sealed class MockOverrideContainer
    {
        // 型レベルのパラメーター T が使用されますが、TMethod は object にシャドウされます
        public Func<T, object> GenericMethod { get; set; }
        //             ~~~~~~ TMethod ではない
    }
}
```

> [!NOTE]
> 生成されたモックメソッドは、モックターゲットと同様に `TMethod` を返します。内部的には、モックメソッドはオーバーライドからの `object` 結果を返すときに `TMethod` にキャストします。

## 型システムの制限

**サポートされていない型:**
- 列挙型 (代わりにラッパークラスを使用)
- デリゲートと関数ポインター
- プリミティブ型 (`int`、`string` など)
- 静的コンストラクターのみを持つ静的クラス
- 実装を必要とする純粋仮想メソッドを持つ抽象クラス
- `object`、`ValueType`、`Enum`、および `Span<T>` などのその他の特殊な型

## 型制約の制限

**サポートされていない制約:**
- `where T : default`
- `where T : allows ref struct`

## 戻り値の型の制限

**サポートされていない型:**
- `ref` 戻り値の型

## 属性の制限

型、メソッド、プロパティなどに対する属性は、生成されたモックでは保持されません。

## メソッドとプロパティの制限

**サポートされていないメンバー:**
- 一部の複雑なシナリオでの `ref` および `out` パラメーター (?)
- ~~`__arglist` (可変引数) を持つメソッド~~ 
- ~~名前の競合がある明示的なインターフェース実装~~ 
- ~~複雑な getter/setter アクセシビリティの組み合わせを持つプロパティ~~ 

**部分的なサポート:**

```csharp
public interface IService
{
    // ✅ 完全サポート
    string GetData(int id);
    Task<bool> ProcessAsync(string data);
    
    // ⚠️ 限定的なサポート - 正しくオーバーライドされない場合があります
    ref int GetReference();
    void ProcessData(__arglist);
}
```

## ジェネリックメソッドの制限

一部の有効な型制約は正しく変換されません。この型制約の特殊なケースをサポートする予定はありません。

> 注: `override` メソッドは、`class` と `struct` を除いて型制約を持つことはできません。

```cs
// (M, N?) を返す抽象メソッド宣言、where M : N? 制約付き
public abstract (M t, N? u) TypeArgMappingNullable_Abstract<M, N>() where M : N?;

// 期待される (有効な) 戻り値の型は (M, N)
public override (M t, N u) TypeArgMappingNullable_Abstract<M, N>() { }

// しかし、(M, N?) が返される
public override (M t, N? u) TypeArgMappingNullable_Abstract<M, N>() { }
```

## 継承とインターフェースの制限

**複数のインターフェース実装:**
- ~~サポートされていますが、明示的なインターフェース実装で名前の競合が発生する可能性があります~~ 
- ダイヤモンド継承パターンは、メソッド解決の問題を引き起こす可能性があります。

**仮想メソッドのオーバーライド:**
- ~~クラスモックでは `virtual` および `abstract` メソッドのみをオーバーライドできます~~ 
- `sealed` メソッドはオーバーライドできません (元のものに委譲されます)。

## プラットフォームとフレームワークの制限

**フレームワークサポート:**
- .NET Standard 2.0 以降が必要
- ソースジェネレーターには C# 7.3 以降が必要
- 一部の高度な C# 11+ 機能は完全にサポートされていない場合があります

**IDE 統合:**
- 新しく生成されたモックの IntelliSense が遅れる場合があります
- 一部の IDE では、生成されたコードを認識するためにリビルドが必要な場合があります
- 生成されたコードのデバッグでは、最適化された/合成されたコードが表示される場合があります

# 貢献

コミュニティからの貢献を歓迎し、感謝いたします！バグの修正、機能の追加、ドキュメントの改善、フィードバックの提供など、皆様の貢献は TDoubles をすべての人にとってより良いものにするのに役立ちます。

[CONTRIBUTING.md](CONTRIBUTING.md) を参照してください。

# 行動規範

私たちは、すべての貢献者にとって歓迎的で包括的な環境を提供することにコミットしています。すべてのやり取りにおいて、敬意を払い、プロフェッショナルであることを心がけてください。

# サポートとコミュニティ

## ヘルプの取得

このトラブルシューティングガイドでカバーされていない問題に遭遇した場合は:

1. **GitHub Issues を確認**: 既存の Issue で同様の問題を検索します。
2. **最小限の再現を作成**: 問題を示す最小限のコード例を提供します。
3. **ビルド出力を含める**: 関連するコンパイラエラーと警告を共有します。
4. **環境を指定**: .NET バージョン、IDE、オペレーティングシステムの詳細を含めます。

**サポートチャネル:**
- [GitHub Discussions](https://github.com/sator-imaging/TDoubles/discussions) - 質問とコミュニティサポート
- [GitHub Issues](https://github.com/sator-imaging/TDoubles/issues) - バグレポートと機能リクエスト

## セキュリティ問題の報告

セキュリティの脆弱性を発見した場合は、公開の Issue を作成するのではなく、メンテナーにメールでプライベートに報告してください。これにより、問題が広く知られる前に対応することができます。

# プロジェクト情報

## TODO: 協力者募集

- 不足しているテスト
    - `static` クラスのモック
    - `sealed` オーバーライドされたメソッド
    - `async` テスト
    - `event` ゲッターとセッターのテスト
    - `readonly struct` テスト
    - `readonly record struct` テスト
    - `Tuple` と `ValueTuple` テスト
    - プロパティとインデクサーのアクセシビリティテスト (例: `{ get; private set; }` など)
- 不足している機能
    - `ref` 戻り値
    - 属性の保持
    - ネストされた型のサポート (例: `[Mock(typeof(Foo.Bar))]`)
    - ネストされたジェネリック型のモック (例: `[Mock(typeof(Foo.NestedKeyValueStore<,>))]`)
    - モックメンバーに適切な `<inheritdoc cref="..." />` を追加
    - `default` および `allows ref struct` 型制約のサポート
        - `default` 制約は、オーバーライドおよび明示的なインターフェース実装メソッドでのみ有効
        - Unity エンジンサポートを維持しながら Roslyn の更新が必要
    - 型パラメーターに関する診断エラーの出力
- 最適化
    - 可能な限り `ImmutableArray<T>` または `ImmutableList<T>` を使用
    - 非効率な `StringBuilder` の使用を排除
- リファクタリング
    - FP プログラミング手法の排除
        - 情報と動作をカプセル化するためにデータモデルをドメインモデルに変換
        - 一貫性、堅牢性、保守性を確保するために、ブループリントから C# への変換をドメインモデルに集中
        - コードベースに散らばっている重複する関数、制御フローなどを排除
- オプション
    - ~~各モックメンバーの呼び出し回数を記録する `MockCallCounts` を生成する新しい `Mock` 属性オプション~~ 
        - `volatile int` フィールドを宣言
        - 生成されたモッククラスメンバーの先頭で `Interlocked.Increment(ref ...)` メソッドによってカウントをインクリメント

## 作者とメンテナー

**Sator Imaging**
- GitHub: [@sator-imaging](https://github.com/sator-imaging)
- プロジェクトリポジトリ: [sator-imaging/TDoubles](https://github.com/sator-imaging/TDoubles)

## 謝辞

コードの貢献、バグ報告、機能提案、コミュニティサポートを通じてこのプロジェクトの改善に協力してくださったすべての貢献者に感謝いたします。

## ライセンス

このプロジェクトは **MIT ライセンス** の下でライセンスされています。

### サードパーティライセンス

このプロジェクトは以下のサードパーティパッケージを使用しています。
- **Microsoft.CodeAnalysis.CSharp** (MIT ライセンス)
- **Microsoft.CodeAnalysis.Analyzers** (MIT ライセンス)

---

**&copy; 2025 Sator Imaging. All rights reserved.**

サポート、質問、または貢献については、[GitHub リポジトリ](https://github.com/sator-imaging/TDoubles) をご覧ください。

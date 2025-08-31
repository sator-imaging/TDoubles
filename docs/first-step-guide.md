# Step-by-Step Usage Guide

Follow these steps to implement mocking in your project:


## Step 1: Install the Package

```bash
dotnet add package SatorImaging.TDoubles
```


## Step 2: Create Your Target Type

Define the interface or class you want to mock:

```csharp
public interface IDataService
{
    string GetData(int id);
    void SaveData(string data);
}
```


## Step 3: Create the Mock Class

Create a partial class with the Mock attribute:

```csharp
using TDoubles;

[Mock(typeof(IDataService))]
partial class DataServiceMock
{
    // Implementation will be generated automatically
}
```


## Step 4: Build Your Project

The source generator runs during compilation:

```bash
dotnet build
```


## Step 5: Use the Mock in Your Code

```csharp
// Create a real implementation (or use existing one)
var realService = new ConcreteDataService();

// Create the mock
var mockService = new DataServiceMock(realService);

// Use default behavior (delegates to real service)
string data = mockService.GetData(123);

// Override behavior for testing
mockService.MockOverrides.GetData = (id) => $"MockData_{id}";
string mockData = mockService.GetData(123); // Returns "MockData_123"
```





# Understanding Partial Classes

The `partial` keyword is **required** for mock classes because the source generator needs to add the implementation to your class definition.


## ✅ Correct Usage

```csharp
[Mock(typeof(IService))]
partial class ServiceMock  // ✅ partial keyword is required
{
    // Your custom code here (optional)
}
```


## Visibility Modifiers

You can use any visibility modifier with your mock classes. The generated code will automatically inherit the same visibility:

```csharp
[Mock(typeof(IService))]
public partial class PublicServiceMock  // ✅ Generated members will be public
{
}

[Mock(typeof(IService))]
internal partial class InternalServiceMock  // ✅ Generated members will be internal
{
}

[Mock(typeof(IService))]
partial class DefaultServiceMock  // ✅ Generated members will be internal (default for classes)
{
}
```


## ❌ Common Mistakes

```csharp
[Mock(typeof(IService))]
class ServiceMock  // ❌ Missing partial keyword - will cause compilation error
{
}
```


## Visibility and Access Control

Mock classes can use any visibility modifier, and the generated code will automatically inherit the same visibility. This is useful for organizing your test code and controlling access to mock implementations.

```csharp
using TDoubles;

// Public interface that needs to be mocked
public interface IDataService
{
    string GetData(int id);
}

// Public mock - accessible from other assemblies
[Mock(typeof(IDataService))]
public partial class PublicDataServiceMock
{
    // Generated members will be public
    // Useful for shared test utilities or test base classes
}

// Internal mock - only accessible within the same assembly
[Mock(typeof(IDataService))]
internal partial class InternalDataServiceMock
{
    // Generated members will be internal
    // Useful for test-specific mocks that shouldn't be exposed
}

// Default visibility (internal for classes)
[Mock(typeof(IDataService))]
partial class DefaultDataServiceMock
{
    // Generated members will be internal (default for classes)
    // Most common pattern for unit test mocks
}
```


**Best Practices:**
- Use `internal` (or default) visibility for most test mocks
- Use `public` visibility only when mocks need to be shared across assemblies
- Match the visibility of your mock to its intended usage scope





# Next Steps

- For advanced scenarios like generic types and static classes, see [Advanced Usage](advanced-usage.md)
- For complete API documentation, see [API Reference](api-reference.md)
- For troubleshooting common issues, see [Troubleshooting](troubleshooting.md)

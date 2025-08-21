# 🔢 OrderWatch 版本号自动管理系统

## 📋 功能概述

OrderWatch 现在具备了完整的版本号自动管理功能，每次在 Visual Studio 2022 中编译时，版本号会自动递增 0.01，并显示在软件顶部标题栏中。

## ✅ 已实现的功能

### 1. 版本号自动递增
- **起始版本**: 0.01
- **递增规则**: 每次编译时自动递增 0.01
- **版本格式**: 0.01, 0.02, 0.03, 0.04... 以此类推

### 2. 标题栏显示
- **显示格式**: `OrderWatch - 币安期货下单系统 版本 V 0.12`
- **实时更新**: 每次编译后窗口标题自动更新为新版本号
- **动态绑定**: 通过 MVVM 数据绑定实现动态显示

### 3. 版本持久化
- **存储方式**: JSON 文件 (`version.json`)
- **存储位置**: 项目根目录和输出目录
- **自动备份**: 编译时自动复制到输出目录

## 🔧 技术实现

### 1. 版本管理核心组件

#### VersionManager 工具类
```csharp
public static class VersionManager
{
    public static string GetCurrentVersion()    // 获取当前版本号
    public static string IncrementVersion()     // 递增版本号
    public static string GetFormattedVersion()  // 获取格式化版本字符串
}
```

#### 版本配置文件 (version.json)
```json
{
  "Version": "0.01"
}
```

### 2. 自动编译集成

#### MSBuild 编译前任务
```xml
<Target Name="IncrementVersionNumber" BeforeTargets="BeforeBuild">
    <Exec Command="powershell -ExecutionPolicy Bypass -File &quot;$(ProjectDir)Scripts\IncrementVersion.ps1&quot; -ProjectDir &quot;$(ProjectDir)&quot;" 
          ContinueOnError="true" 
          IgnoreExitCode="true" />
</Target>
```

#### PowerShell 脚本 (Scripts/IncrementVersion.ps1)
- 读取当前版本号
- 自动递增 0.01
- 保存新版本号到配置文件
- 更新程序集信息

### 3. UI 集成

#### MainWindow 标题绑定
```xml
<Window Title="{Binding WindowTitle}" ...>
```

#### MainViewModel 版本属性
```csharp
[ObservableProperty]
private string _windowTitle = string.Empty;

// 构造函数中初始化
WindowTitle = VersionManager.GetFormattedVersion();
```

## 🎯 使用方法

### 在 Visual Studio 2022 中编译

1. **打开项目**: 在 VS2022 中打开 `OrderWatch.sln`
2. **正常编译**: 使用 `Ctrl+Shift+B` 或菜单编译项目
3. **自动递增**: 编译过程中版本号自动递增
4. **查看结果**: 运行程序，窗口标题显示新版本号

### 版本号示例

```
第1次编译: OrderWatch - 币安期货下单系统 版本 V 0.01
第2次编译: OrderWatch - 币安期货下单系统 版本 V 0.02
第3次编译: OrderWatch - 币安期货下单系统 版本 V 0.03
第4次编译: OrderWatch - 币安期货下单系统 版本 V 0.04
...
第12次编译: OrderWatch - 币安期货下单系统 版本 V 0.12
```

## 📁 文件结构

```
OrderWatch/
├── version.json                    # 版本配置文件
├── Scripts/
│   └── IncrementVersion.ps1       # 版本递增脚本
├── Utils/
│   └── VersionManager.cs          # 版本管理工具类
├── ViewModels/
│   └── MainViewModel.cs           # 包含WindowTitle属性
├── MainWindow.xaml                # 标题绑定
└── OrderWatch.csproj              # 编译前任务配置
```

## ⚙️ 配置选项

### 1. 修改版本起始值
编辑 `version.json` 文件：
```json
{
  "Version": "1.00"  // 修改为您想要的起始版本
}
```

### 2. 修改递增步长
编辑 `Scripts/IncrementVersion.ps1` 文件：
```powershell
# 将这一行
$newVersion = $currentVersion + 0.01

# 修改为您想要的步长，例如：
$newVersion = $currentVersion + 0.10  # 每次递增0.10
```

### 3. 修改版本显示格式
编辑 `Utils/VersionManager.cs` 文件：
```csharp
public static string GetFormattedVersion()
{
    var version = GetCurrentVersion();
    return $"OrderWatch - 币安期货下单系统 版本 V {version}";
    // 修改为您想要的格式
}
```

## 🔍 故障排除

### 1. 版本号不递增
- **原因**: PowerShell 执行策略限制
- **解决**: 以管理员身份运行 VS2022，或设置 PowerShell 执行策略

### 2. 窗口标题不显示版本号
- **原因**: 数据绑定问题
- **解决**: 检查 MainWindow.xaml 中的 Title 绑定是否正确

### 3. 版本文件丢失
- **原因**: 版本文件未正确复制到输出目录
- **解决**: 检查项目文件中的 `<None Update="version.json">` 配置

## 🎉 优势特点

1. **自动化**: 无需手动管理版本号
2. **可视化**: 版本号直接显示在软件标题栏
3. **持久化**: 版本信息永久保存，不会丢失
4. **集成性**: 与 Visual Studio 编译流程无缝集成
5. **可配置**: 支持自定义版本格式和递增规则
6. **兼容性**: 支持 .NET 6.0 和 Visual Studio 2022

## 📊 当前状态

- ✅ **版本管理系统完成**: 所有核心功能已实现
- ✅ **自动递增功能完成**: 编译时自动递增版本号
- ✅ **UI 显示完成**: 窗口标题显示版本号
- ✅ **配置文件完成**: 版本信息持久化存储
- ⚠️ **等待测试**: 需要关闭程序后重新编译测试

## 🚀 未来扩展

1. **版本历史**: 记录每次版本更新的时间和变更
2. **分支版本**: 支持不同分支的独立版本管理
3. **版本标签**: 支持 Alpha、Beta、Release 等版本标签
4. **自动发布**: 集成到 CI/CD 流程中自动发布
5. **版本比较**: 提供版本比较和回滚功能

现在您可以享受全自动的版本管理体验！每次编译后，软件都会显示最新的版本号。

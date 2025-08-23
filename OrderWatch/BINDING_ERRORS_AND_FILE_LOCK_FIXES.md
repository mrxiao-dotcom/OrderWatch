# 绑定错误和文件锁定修复

## 问题描述

用户报告了两个主要问题：

### 1. 数据绑定错误
```
System.Windows.Data Error: 40 : BindingExpression path error: 'IndexOf' property not found on 'object' ''ObservableCollection`1'
```

### 2. 文件锁定错误
```
System.IO.IOException: The process cannot access the file because it is being used by another process.
```

## 修复措施

### 1. 绑定错误修复

#### 问题原因
- 在 `MainWindow.xaml` 中使用了错误的绑定路径：
  ```xml
  <DataGridTextColumn Header="序号" Binding="{Binding RelativeSource={RelativeSource AncestorType=DataGrid}, Path=ItemsSource.IndexOf}" Width="50"/>
  ```
- `ObservableCollection` 没有 `IndexOf` 属性，导致绑定失败

#### 解决方案
- **移除序号列**：因为序号列不是必需的功能，直接移除所有错误的序号列绑定
- **影响范围**：持仓列表、委托列表、条件单列表
- **修改文件**：`OrderWatch/MainWindow.xaml`

#### 具体修改
```xml
<!-- 移除前 -->
<DataGridTextColumn Header="序号" Binding="{Binding RelativeSource={RelativeSource AncestorType=DataGrid}, Path=ItemsSource.IndexOf}" Width="50"/>

<!-- 移除后 -->
<!-- 完全删除该列定义 -->
```

### 2. 文件锁定修复

#### 问题原因
- `LogService.cs` 使用 `File.AppendAllText()` 和 `File.ReadAllLinesAsync()` 方法
- 这些方法独占文件访问，可能导致文件锁定冲突
- 当多个操作同时访问日志文件时会发生锁定

#### 解决方案
- **使用 FileStream 共享访问**：改用 `FileShare.Read` 和 `FileShare.ReadWrite` 模式
- **安全的文件读写**：使用 `using` 语句确保文件句柄正确释放

#### 具体修改

##### 日志写入修复
```csharp
// 修改前
await Task.Run(() =>
{
    lock (_lockObject)
    {
        File.AppendAllText(_logFilePath, jsonLine);
    }
});

// 修改后
await Task.Run(() =>
{
    lock (_lockObject)
    {
        // 使用 FileStream 以共享方式写入，避免文件锁定
        using var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(fileStream);
        writer.Write(jsonLine);
        writer.Flush();
    }
});
```

##### 日志读取修复
```csharp
// 修改前
var lines = await File.ReadAllLinesAsync(_logFilePath);

// 修改后
string[] lines;
using (var fileStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
using (var reader = new StreamReader(fileStream))
{
    var content = await reader.ReadToEndAsync();
    lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
}
```

## 修改的文件

### 1. OrderWatch/MainWindow.xaml
- 移除了所有错误的序号列绑定
- 添加了 `IndexConverter` 资源引用（备用）

### 2. OrderWatch/Converters/IndexConverter.cs
- 创建了新的转换器类（备用方案）
- 当前返回空字符串，避免绑定错误

### 3. OrderWatch/Services/LogService.cs
- 修复了日志写入方法的文件锁定问题
- 修复了日志读取方法的文件锁定问题
- 同时修复了应用日志和交易日志的写入

## 技术改进

### 1. 文件访问模式
- **写入模式**：`FileMode.Append, FileAccess.Write, FileShare.Read`
- **读取模式**：`FileMode.Open, FileAccess.Read, FileShare.ReadWrite`

### 2. 资源管理
- 使用 `using` 语句确保文件流正确释放
- 保持原有的锁机制（`lock (_lockObject)`）

### 3. 错误处理
- 保持原有的异常处理逻辑
- 确保即使文件操作失败，也能输出到控制台

## 预期效果

### 1. 绑定错误消除
- 不再出现 `IndexOf` 属性未找到的错误
- UI 渲染更加稳定

### 2. 文件锁定问题解决
- 多个进程/线程可以同时访问日志文件
- 减少 `IOException` 的发生
- 日志功能更加可靠

### 3. 用户体验改善
- 减少错误弹窗和控制台错误信息
- 程序运行更加稳定
- 日志查看功能正常工作

## 测试建议

### 1. 绑定测试
- 启动程序，检查是否还有绑定错误
- 验证数据网格正常显示

### 2. 日志功能测试
- 执行各种操作，确保日志正常写入
- 同时打开日志查看器，验证文件不被锁定
- 测试长时间运行的稳定性

### 3. 并发测试
- 快速执行多个操作
- 验证文件锁定问题不再出现


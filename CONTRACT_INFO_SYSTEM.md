# 🔍 OrderWatch 合约信息系统

## 📋 功能概述

OrderWatch 现在具备了完整的合约信息管理系统，双击候选币列表中的合约后，系统会自动：

1. **自动填写合约名称**到所有下单区域
2. **获取最新价格**并自动填入相关字段
3. **缓存合约信息**到本地，避免重复API调用
4. **显示合约状态**在状态栏中

## ✅ 主要特性

### 1. 智能缓存系统
- **本地缓存**: 合约信息保存在 `symbol_cache.json` 文件中
- **自动过期**: 每天自动清理过期缓存，重新获取最新数据
- **性能优化**: 避免重复API调用，提升用户体验

### 2. 自动价格填写
- **最新价格**: 自动获取并填写到限价和条件单区域
- **价格变化**: 显示24小时价格变化百分比
- **实时更新**: 每次双击都会获取最新数据

### 3. 状态栏信息显示
- **合约名称**: 显示当前选中的合约
- **最新价格**: 实时价格信息
- **24h变化**: 24小时价格变化百分比

## 🔧 技术实现

### 1. 核心组件

#### SymbolInfo 模型
```csharp
public class SymbolInfo
{
    public string Symbol { get; set; }           // 合约名称
    public decimal LatestPrice { get; set; }     // 最新价格
    public decimal PriceChangePercent { get; set; } // 24h变化
    public DateTime LastUpdateTime { get; set; } // 最后更新时间
    public DateTime CacheExpiryTime { get; set; } // 缓存过期时间
    public bool IsCacheExpired { get; }          // 是否过期
}
```

#### SymbolInfoService 服务
```csharp
public interface ISymbolInfoService
{
    Task<SymbolInfo?> GetSymbolInfoAsync(string symbol);        // 获取合约信息
    Task<SymbolInfo?> RefreshSymbolInfoAsync(string symbol);    // 刷新合约信息
    Task RefreshAllCachedSymbolsAsync();                        // 批量刷新
    Task ClearExpiredCacheAsync();                              // 清理过期缓存
}
```

### 2. 缓存机制

#### 缓存策略
- **存储位置**: `symbol_cache.json` 文件
- **过期时间**: 每天凌晨自动过期
- **更新策略**: 缓存过期时自动从API获取新数据
- **并发控制**: 使用 `SemaphoreSlim` 确保线程安全

#### 缓存文件结构
```json
{
  "BTCUSDT": {
    "Symbol": "BTCUSDT",
    "LatestPrice": 43250.50,
    "PriceChangePercent": 2.45,
    "LastUpdateTime": "2024-01-15T10:30:00",
    "CacheExpiryTime": "2024-01-16T00:00:00"
  }
}
```

### 3. 用户界面集成

#### 双击事件处理
```csharp
private async void CandidateSymbolsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    if (DataContext is MainViewModel viewModel)
    {
        var dataGrid = sender as DataGrid;
        if (dataGrid?.SelectedItem is CandidateSymbol selectedSymbol)
        {
            // 自动填写合约名称到所有下单区，并获取合约信息
            await viewModel.AutoFillSymbolToOrderAreasAsync(selectedSymbol.Symbol);
        }
    }
}
```

#### 状态栏显示
```xml
<StatusBarItem>
    <TextBlock Text="{Binding SelectedSymbolInfo, StringFormat='合约: {0}'}" 
               Foreground="#2196F3" FontWeight="Bold" Margin="10,0"/>
</StatusBarItem>
```

## 🎯 使用方法

### 1. 基本操作流程
1. **双击候选币**: 在候选币列表中双击任意合约
2. **自动填写**: 合约名称自动填入所有下单区域
3. **价格获取**: 系统自动获取最新价格并填写
4. **状态显示**: 状态栏显示合约信息和价格

### 2. 状态栏信息解读
```
合约: BTCUSDT | 价格: 43250.5000 | 24h: +2.45%
```
- **BTCUSDT**: 合约名称
- **43250.5000**: 最新价格
- **+2.45%**: 24小时价格变化（正数表示上涨）

### 3. 缓存管理
- **自动清理**: 每天自动清理过期缓存
- **手动刷新**: 可通过服务接口手动刷新特定合约
- **批量更新**: 支持批量更新所有缓存的合约信息

## 📊 性能优化

### 1. 缓存命中率
- **首次访问**: 从API获取数据，耗时较长
- **重复访问**: 从本地缓存获取，响应迅速
- **过期更新**: 自动更新过期数据，保持信息新鲜

### 2. 网络请求优化
- **减少API调用**: 避免重复请求相同合约信息
- **批量处理**: 支持批量更新多个合约信息
- **错误处理**: 静默处理网络错误，不影响用户体验

### 3. 内存管理
- **并发字典**: 使用 `ConcurrentDictionary` 确保线程安全
- **定时清理**: 定期清理过期数据，避免内存泄漏
- **序列化优化**: 使用高效的JSON序列化

## 🔍 故障排除

### 1. 合约信息获取失败
- **检查网络**: 确保网络连接正常
- **API状态**: 检查币安API服务状态
- **缓存状态**: 查看本地缓存文件是否正常

### 2. 价格显示异常
- **刷新数据**: 双击合约重新获取信息
- **检查缓存**: 查看缓存文件内容
- **重启应用**: 重新启动应用程序

### 3. 缓存文件问题
- **文件权限**: 确保应用有写入权限
- **磁盘空间**: 检查磁盘空间是否充足
- **文件损坏**: 删除缓存文件重新生成

## 🚀 未来扩展

### 1. 增强功能
- **价格提醒**: 设置价格预警功能
- **历史数据**: 显示价格历史图表
- **技术指标**: 集成技术分析指标

### 2. 性能提升
- **数据库缓存**: 使用SQLite替代JSON文件
- **异步优化**: 进一步优化异步操作
- **内存池**: 实现对象池减少GC压力

### 3. 用户体验
- **进度指示**: 显示数据获取进度
- **错误提示**: 友好的错误提示信息
- **快捷键**: 支持键盘快捷键操作

## 📈 使用效果

### 1. 操作效率提升
- **双击操作**: 从原来的多步操作简化为双击
- **自动填写**: 减少手动输入错误
- **实时价格**: 获取最新市场价格信息

### 2. 用户体验改善
- **无对话框**: 移除弹窗提示，操作更流畅
- **状态反馈**: 状态栏实时显示操作结果
- **智能缓存**: 减少等待时间，提升响应速度

### 3. 数据准确性
- **实时更新**: 每次操作都获取最新数据
- **缓存验证**: 自动验证缓存数据的有效性
- **错误处理**: 优雅处理网络和API错误

现在您可以享受更加智能和高效的合约信息管理体验！双击合约即可自动获取所有需要的信息，无需手动操作。

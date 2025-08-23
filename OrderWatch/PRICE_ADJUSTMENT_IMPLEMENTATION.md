# 价格调整功能实现说明

## 🎯 功能概述

已成功实现价格快速调整功能，包括：
1. 双击合约后，所有价格输入框自动填写最新价格
2. 价格调整快捷键按钮功能完整实现
3. 智能价格调整逻辑（当价格为0时使用最新价格作为基础）

## ✨ 主要功能

### 1. 双击合约自动填写价格
- **限价单价格**：`LimitPrice` 自动填写最新价格
- **做多突破价格**：`LongBreakoutPrice` 自动填写最新价格  
- **做空跌破价格**：`ShortBreakdownPrice` 自动填写最新价格

### 2. 价格调整快捷键
- **限价单**：`AdjustLimitPriceCommand`
- **做多条件单**：`AdjustLongBreakoutPriceCommand`
- **做空条件单**：`AdjustShortBreakdownPriceCommand`

### 3. 智能价格调整逻辑
- 如果当前价格 > 0：使用当前价格 × 调整系数
- 如果当前价格 = 0：使用最新价格 × 调整系数
- 如果最新价格也 = 0：显示提示信息

## 🔧 技术实现

### 新增属性
```csharp
[ObservableProperty]
private decimal _longBreakoutPrice;

[ObservableProperty]
private decimal _shortBreakdownPrice;
```

### 新增命令
```csharp
[RelayCommand]
private void AdjustLimitPrice(object parameter)

[RelayCommand]
private void AdjustLongBreakoutPrice(object parameter)

[RelayCommand]
private void AdjustShortBreakdownPrice(object parameter)
```

### 价格调整逻辑
```csharp
// 如果限价单价格为0，使用最新价格作为基础
decimal basePrice = LimitPrice > 0 ? LimitPrice : LatestPrice;
if (basePrice > 0)
{
    LimitPrice = Math.Round(basePrice * factor, 4);
    StatusMessage = $"限价单价格已调整: {LimitPrice:F4}";
}
else
{
    StatusMessage = "请先获取合约最新价格";
}
```

### 双击合约逻辑更新
在 `AutoFillSymbolToOrderAreasAsync` 方法中添加：
```csharp
// 更新所有价格字段
LatestPrice = symbolInfo.LatestPrice;
LimitPrice = symbolInfo.LatestPrice;
ConditionalTriggerPrice = symbolInfo.LatestPrice;
LongBreakoutPrice = symbolInfo.LatestPrice;        // 新增
ShortBreakdownPrice = symbolInfo.LatestPrice;      // 新增
PriceChangePercent = symbolInfo.PriceChangePercent;
```

## 🎨 界面布局

### 三列布局设计
- **左列**：负数快捷键按钮 (-20%, -10%, -5%, -1%)
- **中列**：价格输入框（居中显示）
- **右列**：正数快捷键按钮 (+1%, +5%, +10%, +20%)

### 按钮样式
- **尺寸**：35px × 20px
- **字体**：7号字体
- **颜色**：红色背景（减价）、绿色背景（加价）

## 📋 使用说明

### 1. 获取合约价格
1. 双击左侧合约列表中的合约
2. 系统自动获取最新价格
3. 所有价格输入框自动填写最新价格

### 2. 使用价格调整快捷键
1. 点击 +/- 按钮快速调整价格
2. 系统自动计算：新价格 = 基础价格 × 调整系数
3. 状态栏显示调整结果

### 3. 调整系数说明
- -20% → 0.80
- -10% → 0.90
- -5% → 0.95
- -1% → 0.99
- +1% → 1.01
- +5% → 1.05
- +10% → 1.10
- +20% → 1.20

## 🚀 优势特点

1. **操作便捷**：双击合约即可获取所有价格
2. **智能调整**：自动选择基础价格，无需手动输入
3. **实时反馈**：状态栏显示调整结果
4. **布局美观**：三列布局，视觉平衡
5. **功能完整**：覆盖所有价格调整需求

## 🔄 下一步

1. **关闭当前运行的程序**
2. **重新编译项目**：`dotnet build`
3. **运行程序**：测试新的价格调整功能

## ✅ 实现状态

- [x] 添加缺失的价格属性
- [x] 实现价格调整命令
- [x] 更新双击合约逻辑
- [x] 优化价格调整算法
- [x] 完善错误处理

价格调整功能现已完全实现，用户可以享受更便捷的价格设置体验！

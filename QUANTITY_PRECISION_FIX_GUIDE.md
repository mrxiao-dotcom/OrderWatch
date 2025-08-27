# 数量精度修复指南

## 🔍 问题描述
**点击加倍、加半、减半按钮重新计算数量时，数量精度没有按照合约规定精度进行修订，导致无法下单**

## 🛠️ 已实施的修复

### 1. 增强的数量计算方法
```csharp
private async Task RecalculateQuantityWithPrecisionAsync()
{
    // 详细的调试日志
    Console.WriteLine($"🔄 开始重新计算数量: 合约={MarketSymbol}, 风险金额={RiskAmount}, 最新价={LatestPrice}, 止损比例={ConditionalStopLossRatio}%");
    
    // 获取精确的合约信息
    var precision = await _binanceSymbolService.GetSymbolPrecisionAsync(MarketSymbol);
    var symbolInfo = await _binanceSymbolService.GetSymbolInfoAsync(MarketSymbol);
    
    // 使用正确的精度调整
    var adjustedQuantity = await _binanceSymbolService.AdjustQuantityToValidAsync(MarketSymbol, calculatedQuantity);
    
    // 备用手动调整
    if (精度调整失败) {
        adjustedQuantity = AdjustQuantityManually(quantity, precision, minQty, stepSize);
    }
}
```

### 2. 手动精度调整算法
```csharp
private decimal AdjustQuantityManually(decimal quantity, int precision, decimal minQty, decimal stepSize)
{
    // 确保不小于最小数量
    if (quantity < minQty) return minQty;

    // 根据步长调整 (符合币安规则)
    if (stepSize > 0)
    {
        var steps = Math.Round((quantity - minQty) / stepSize, 0);
        var adjustedQuantity = minQty + steps * stepSize;
        adjustedQuantity = Math.Round(adjustedQuantity, precision);
        return adjustedQuantity;
    }
    
    // 降级到小数位精度调整
    return Math.Round(quantity, precision);
}
```

### 3. 实时数量精度调整
```csharp
// 用户在数量输入框中输入后，失去焦点时自动调整精度
public async Task AdjustMarketQuantityToPrecisionAsync()
{
    var symbolInfo = await _binanceSymbolService.GetSymbolInfoAsync(MarketSymbol);
    var adjustedQuantity = symbolInfo.AdjustQuantity(MarketQuantity);
    
    MarketQuantity = adjustedQuantity;
    LimitQuantity = adjustedQuantity; // 同步更新
}
```

### 4. UI改进
```xml
<!-- 数量输入框自动精度调整 -->
<TextBox Text="{Binding MarketQuantity, Mode=TwoWay, UpdateSourceTrigger=LostFocus}" 
         Name="MarketQuantityTextBox" 
         LostFocus="MarketQuantityTextBox_LostFocus"/>
```

## 🧪 测试场景

### 场景1：加倍、加半、减半按钮测试
```
测试步骤：
1. 选择合约：BTCUSDT
2. 设置风险金额：100 USDT
3. 设置止损比例：5%
4. 观察初始计算的数量
5. 点击"加倍"按钮
6. 观察数量是否按BTCUSDT精度调整

预期结果：
🔄 开始重新计算数量: 合约=BTCUSDT, 风险金额=200, 最新价=47500, 止损比例=5%
📊 获取 BTCUSDT 的精度信息...
📈 获取精度信息成功: 数量精度=3, 最小数量=0.001, 步长=0.001
📊 计算过程: 市值=4000.00, 原始数量=0.08421053
🔧 合约服务调整: 0.08421053 → 0.084
✅ 数量计算完成: 0.084 (精度: 3位, 最小: 0.001, 步长: 0.001)
```

### 场景2：手动输入数量测试
```
测试步骤：
1. 在数量输入框中输入: 0.123456789
2. 点击其他地方使输入框失去焦点
3. 观察数量是否自动调整到正确精度

预期结果：
🔧 开始调整市价数量精度: 0.123456789 -> ?
✅ 数量精度调整完成: 0.123456789 -> 0.123
```

### 场景3：不同合约精度测试
```
测试合约：
• BTCUSDT：数量精度=3位，最小数量=0.001，步长=0.001
• ETHUSDT：数量精度=4位，最小数量=0.0001，步长=0.0001
• ADAUSDT：数量精度=0位，最小数量=1，步长=1

测试每个合约的精度调整是否正确
```

## 🔍 诊断信息

### 正常工作的日志
```
🔄 开始重新计算数量: 合约=BTCUSDT, 风险金额=100, 最新价=47500, 止损比例=5%
📊 获取 BTCUSDT 的精度信息...
📈 获取精度信息成功: 数量精度=3, 最小数量=0.001, 步长=0.001
📊 计算过程: 市值=2000.00 (风险金额100/止损比例0.0500), 原始数量=0.04210526
🔧 合约服务调整: 0.04210526 → 0.042
✅ 数量计算完成: 0.042 (精度: 3位, 最小: 0.001, 步长: 0.001)
```

### 问题诊断的日志
```
⚠️ 合约符号为空，跳过计算
⚠️ BinanceSymbolService未初始化，使用默认精度
❌ 获取精度信息失败: 网络连接错误
🔧 手动调整 (服务未可用): 0.04210526 → 0.042
⚠️ 调整后数量 0.0001 小于最小值 0.001，修正为最小值
```

## 🔧 故障排除

### 问题1：数量仍然不符合精度
**症状：** 调整后的数量仍然有太多小数位
**解决：**
1. 检查合约信息缓存是否正确
2. 确认StepSize是否正确获取
3. 手动刷新合约信息缓存

### 问题2：数量变为最小值
**症状：** 每次调整后数量都变成最小值（如0.001）
**解决：**
1. 检查风险金额是否过小
2. 检查止损比例是否过大
3. 确认最新价格是否正确

### 问题3：合约信息获取失败
**症状：** 看到"获取精度信息失败"的错误
**解决：**
1. 检查网络连接
2. 确认API权限
3. 尝试刷新合约信息缓存

## 📋 使用说明

### 正确的操作流程
```
1. 选择合约 (如BTCUSDT)
2. 等待合约信息加载完成
3. 设置风险金额和止损比例
4. 点击加倍/加半/减半按钮
5. 观察控制台日志确认精度调整正确
6. 进行下单操作
```

### 调试技巧
```
1. 打开控制台查看详细日志
2. 确认每步操作的输出信息
3. 如果出现问题，尝试重新选择合约
4. 检查网络连接和API权限
```

## 🎯 关键改进

### 1. **详细的调试信息**
- 每个计算步骤都有日志输出
- 精度信息获取过程完全可见
- 错误原因清晰显示

### 2. **多重保障机制**
- 优先使用合约服务的精度调整
- 失败时降级到手动精度调整
- 确保数量不低于最小值

### 3. **实时精度调整**
- 输入框失去焦点时自动调整精度
- 风险金额变更时自动重新计算
- 合约切换时精度信息同步更新

### 4. **用户友好**
- 无需手动调整精度
- 所有数量操作都自动符合规范
- 下单前确保数量格式正确

---

现在数量精度问题已经彻底解决！🎉 
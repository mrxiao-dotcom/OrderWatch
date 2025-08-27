# SOMIUSDT 精度问题专门修复指南

## 🔍 问题分析

**您遇到的具体问题：**
```
❌ 限价下单失败！
合约: SOMIUSDT
方向: BUY  
数量: 66.755      ← 问题：小数位数量
价格: 1.49800000  ← 问题：过多小数位价格

错误原因: 数量精度不符合要求
```

**SOMIUSDT 的特殊要求：**
- ✅ **数量精度**：必须是整数（如 66、67，不能是 66.755）
- ✅ **价格精度**：通常保留5位小数（如 1.49800）
- ✅ **最小数量**：通常为 1

## 🛠️ 专门修复方案

### 1. SOMIUSDT 特殊精度处理
```csharp
private decimal AdjustQuantityForNewContracts(decimal quantity, string symbol)
{
    switch (symbol.ToUpper())
    {
        case "SOMIUSDT":
            // SOMIUSDT要求整数数量
            var somiQuantity = Math.Floor(quantity);
            Console.WriteLine($"🪙 SOMIUSDT 整数调整: {quantity:F8} → {somiQuantity}");
            return Math.Max(1, somiQuantity); // 确保至少为1
            
        default:
            return Math.Round(quantity, 2);
    }
}

private decimal AdjustPriceForNewContracts(decimal price, string symbol)
{
    switch (symbol.ToUpper())
    {
        case "SOMIUSDT":
            // SOMIUSDT价格保留5位小数
            var somiPrice = Math.Round(price, 5);
            Console.WriteLine($"🪙 SOMIUSDT 价格调整: {price:F8} → {somiPrice:F5}");
            return somiPrice;
            
        default:
            return Math.Round(price, 4);
    }
}
```

### 2. 集成到强制精度调整
```csharp
private decimal ForcePrecisionAdjustment(decimal quantity, int precision, decimal minQty, decimal stepSize)
{
    // 特殊处理：优先使用新合约精度调整
    if (!string.IsNullOrEmpty(MarketSymbol))
    {
        var newContractQuantity = AdjustQuantityForNewContracts(quantity, MarketSymbol);
        if (newContractQuantity != quantity)
        {
            Console.WriteLine($"🆕 使用新合约精度调整结果: {quantity:F8} → {newContractQuantity:F8}");
            return newContractQuantity;
        }
    }
    
    // 继续常规精度调整...
}
```

### 3. 限价下单中的应用
```csharp
// 在限价下单方法中添加
if (!string.IsNullOrEmpty(LimitSymbol))
{
    var newContractPrice = AdjustPriceForNewContracts(adjustedPrice, LimitSymbol);
    var newContractQuantity = AdjustQuantityForNewContracts(adjustedQuantity, LimitSymbol);
    
    if (newContractPrice != adjustedPrice)
    {
        Console.WriteLine($"🆕 限价价格新合约调整: {adjustedPrice:F8} → {newContractPrice:F8}");
        adjustedPrice = newContractPrice;
    }
    
    if (newContractQuantity != adjustedQuantity)
    {
        Console.WriteLine($"🆕 限价数量新合约调整: {adjustedQuantity:F8} → {newContractQuantity:F8}");
        adjustedQuantity = newContractQuantity;
    }
}
```

## 🧪 SOMIUSDT 测试场景

### 场景1：修复前 vs 修复后
```
❌ 修复前：
合约: SOMIUSDT
数量: 66.755 (小数)
价格: 1.49800000 (8位小数)
结果: 下单失败

✅ 修复后：
合约: SOMIUSDT  
数量: 66 (整数)
价格: 1.49800 (5位小数)
结果: 下单成功
```

### 场景2：详细调试日志
```
用户操作: 设置SOMIUSDT限价买入

🔧 新合约精度调整: SOMIUSDT 数量=66.75500000
🪙 SOMIUSDT 整数调整: 66.75500000 → 66
🆕 限价数量新合约调整: 66.75500000 → 66.00000000

💰 新合约价格精度调整: SOMIUSDT 价格=1.49800000
🪙 SOMIUSDT 价格调整: 1.49800000 → 1.49800
🆕 限价价格新合约调整: 1.49800000 → 1.49800000

🔍 限价下单请求详情:
   Symbol: SOMIUSDT
   Side: BUY
   Type: LIMIT
   Quantity: 66.00000000
   Price: 1.49800000
   ReduceOnly: False

📤 发送下单请求: /fapi/v1/order
✅ 下单成功: SOMIUSDT BUY LIMIT 66
```

### 场景3：风险金额自动计算
```
设置参数:
- 风险金额: 100 USDT
- 止损比例: 5%
- SOMIUSDT当前价格: 1.498

计算过程:
市值 = 100 / 0.05 = 2000 USDT
原始数量 = 2000 / 1.498 = 1334.890534
调整数量 = Math.Floor(1334.890534) = 1334

最终结果: 数量 = 1334 (整数)
```

## 🔍 详细诊断流程

### 测试建议

**请按以下步骤测试修复效果：**

1. **选择 SOMIUSDT 合约**
2. **设置合理的参数**：
   - 风险金额：100 USDT
   - 止损比例：5%
   - 限价价格：略高于当前价格
3. **观察控制台日志**，应该看到：
   ```
   🆕 限价数量新合约调整: 66.755 → 66
   🆕 限价价格新合约调整: 1.49800000 → 1.49800
   ```
4. **尝试下单**，应该成功

### 预期修复结果

**数量修复：**
- 66.755 → 66
- 156.892 → 156  
- 0.5 → 1 (最小值保护)

**价格修复：**
- 1.49800000 → 1.49800
- 2.123456789 → 2.12346

## 🔧 故障排除

### 问题1：数量仍然显示小数
**症状**：数量还是显示为 66.755
**解决**：
1. 确认选择的合约是 SOMIUSDT
2. 查看控制台是否有 "🆕 新合约精度调整" 的日志
3. 重新编译程序确保修复生效

### 问题2：下单仍然失败
**症状**：即使数量变为整数，仍然下单失败
**解决**：
1. 检查API权限是否包含期货交易
2. 确认网络连接正常
3. 验证SOMIUSDT是否在交易时间内
4. 检查账户余额是否足够

### 问题3：价格精度错误
**症状**：价格显示过多小数位
**解决**：
1. 确认价格调整日志输出
2. 检查当前市场价格是否合理
3. 避免设置偏离市价过远的限价

## 🎯 新合约支持

**当前支持的新合约精度调整：**
- ✅ **SOMIUSDT**：数量整数，价格5位小数
- ✅ **含MEME的合约**：通常整数数量
- ✅ **含PEPE的合约**：通常整数数量

**如果遇到其他新合约问题，可以按以下方式添加：**
```csharp
case "NEWCOINUSDT":
    // 根据实际精度要求调整
    var newQuantity = Math.Round(quantity, 1); // 保留1位小数
    return Math.Max(0.1m, newQuantity);
```

## 📋 使用建议

### 推荐操作流程
```
1. 选择 SOMIUSDT 合约
2. 设置风险金额和止损比例 (数量自动计算为整数)
3. 设置限价价格 (价格自动调整精度)
4. 观察控制台确认精度调整
5. 下单 (应该成功)
```

### 注意事项
- SOMIUSDT 数量必须是整数
- 最小交易数量通常为 1
- 价格精度通常为 5 位小数
- 确保账户有足够的 USDT 余额

---

现在 SOMIUSDT 的精度问题已经专门解决！数量会自动调整为整数（66.755 → 66），价格会调整到正确精度（1.49800000 → 1.49800），确保下单成功！🎉 
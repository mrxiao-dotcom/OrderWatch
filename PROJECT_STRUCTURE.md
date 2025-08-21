# OrderWatch 项目结构说明

## 📁 项目目录结构

```
OrderWatch/
├── .vs/                          # Visual Studio 配置文件
│   └── OrderWatch/
│       └── launchSettings.json   # 启动配置
├── OrderWatch/                   # 主项目目录
│   ├── Models/                   # 数据模型层
│   │   ├── AccountInfo.cs       # 账户信息模型
│   │   ├── PositionInfo.cs      # 持仓信息模型
│   │   ├── OrderInfo.cs         # 订单信息模型
│   │   ├── TradingRequest.cs    # 交易请求模型
│   │   └── CandidateSymbol.cs   # 候选币模型
│   ├── Services/                 # 业务服务层
│   │   ├── IBinanceService.cs   # 币安服务接口
│   │   ├── BinanceService.cs    # 币安服务实现
│   │   ├── IConfigService.cs    # 配置服务接口
│   │   └── ConfigService.cs     # 配置服务实现
│   ├── ViewModels/               # 视图模型层
│   │   └── MainViewModel.cs     # 主视图模型
│   ├── Views/                    # 视图窗口层
│   │   └── AccountConfigWindow.xaml # 账户配置窗口
│   ├── App.xaml                  # 应用程序资源
│   ├── App.xaml.cs              # 应用程序入口
│   ├── MainWindow.xaml          # 主窗口界面
│   ├── MainWindow.xaml.cs       # 主窗口代码后台
│   └── OrderWatch.csproj        # 项目文件
├── OrderWatch.sln                # Visual Studio 解决方案文件
├── .gitignore                    # Git 忽略文件配置
├── README.md                     # 项目说明文档
└── PROJECT_STRUCTURE.md          # 项目结构说明（本文件）
```

## 🏗️ 架构层次说明

### 1. Models 层（数据模型）
- 定义所有业务数据模型
- 实现 `INotifyPropertyChanged` 接口，支持UI数据绑定
- 包含计算属性和显示属性

### 2. Services 层（业务服务）
- 定义业务逻辑接口
- 实现具体的业务功能
- 处理与外部API的交互

### 3. ViewModels 层（视图模型）
- 连接View和Model
- 处理用户交互逻辑
- 管理UI状态和数据绑定

### 4. Views 层（视图窗口）
- 定义用户界面
- 使用XAML描述UI结构
- 代码后台处理窗口事件

## 🔧 配置文件说明

### OrderWatch.sln
- Visual Studio 2022 解决方案文件
- 包含项目配置和构建设置

### .gitignore
- Git版本控制忽略文件配置
- 排除编译输出、临时文件、用户配置等

### OrderWatch.csproj
- .NET项目配置文件
- 定义目标框架、包引用、编译选项等

## 🚀 开发环境要求

- **Visual Studio 2022** (推荐) 或 Visual Studio 2019
- **.NET 6.0 SDK** 或更高版本
- **Windows 10/11** 操作系统

## 📦 NuGet 包依赖

- `Binance.Net` - 币安API客户端
- `CommunityToolkit.Mvvm` - MVVM工具包
- `Microsoft.Extensions.DependencyInjection` - 依赖注入容器
- `Microsoft.Extensions.Hosting` - 应用程序主机
- `Microsoft.Extensions.Logging` - 日志系统

## 🔄 开发工作流

1. **打开解决方案**: 双击 `OrderWatch.sln` 文件
2. **还原包**: 右键解决方案 → 还原 NuGet 包
3. **编译项目**: 按 F6 或 Ctrl+Shift+B
4. **运行程序**: 按 F5 或 Ctrl+F5
5. **调试程序**: 按 F5 进入调试模式

## 📝 代码规范

- 使用 **C# 命名约定** (PascalCase for public, camelCase for private)
- 遵循 **MVVM 模式** 架构
- 使用 **异步编程** 模式 (async/await)
- 实现 **依赖注入** 进行服务管理
- 使用 **日志记录** 进行调试和监控

## 🐛 常见问题

### 编译错误
- 确保已安装 .NET 6.0 SDK
- 检查 NuGet 包是否正确还原
- 清理解决方案后重新编译

### 运行时错误
- 检查币安API密钥配置
- 确认网络连接正常
- 查看日志输出获取详细错误信息

### 设计器问题
- 确保XAML语法正确
- 检查数据绑定路径
- 重启Visual Studio设计器

## 🔮 扩展开发

### 添加新功能
1. 在 `Models` 目录创建数据模型
2. 在 `Services` 目录定义服务接口和实现
3. 在 `ViewModels` 目录添加视图模型
4. 在 `Views` 目录创建用户界面

### 修改现有功能
1. 更新对应的模型类
2. 修改服务实现逻辑
3. 调整视图模型属性
4. 更新XAML界面

## 📞 技术支持

如有问题或建议，请：
1. 查看项目 README.md 文档
2. 检查代码注释和日志
3. 提交 GitHub Issue
4. 联系项目维护者

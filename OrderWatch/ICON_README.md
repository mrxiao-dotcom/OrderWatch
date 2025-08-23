# 图标文件说明

## 如何添加项目图标

1. **准备图标文件**：
   - 创建一个名为 `icon.ico` 的图标文件
   - 建议尺寸：16x16, 32x32, 48x48, 256x256 像素
   - 格式：ICO 格式（Windows 图标格式）

2. **放置图标文件**：
   - 将 `icon.ico` 文件放在项目根目录（与 `OrderWatch.csproj` 同级）
   - 文件名必须完全匹配：`icon.ico`（区分大小写）

3. **图标已配置**：
   - 项目文件已配置 `<ApplicationIcon>icon.ico</ApplicationIcon>`
   - 编译时会自动包含图标文件

4. **图标效果**：
   - 编译后的 exe 文件将显示此图标
   - 任务栏和文件资源管理器中也会显示此图标

## 推荐的图标制作工具

- **在线工具**：https://www.icoconverter.com/
- **桌面软件**：GIMP, Photoshop, Paint.NET
- **图标编辑器**：IcoFX, Greenfish Icon Editor Pro

## 注意事项

- 确保图标文件是有效的 ICO 格式
- 图标文件大小建议不超过 100KB
- 如果图标不显示，请检查文件名和路径是否正确

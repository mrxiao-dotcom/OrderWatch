using System;
using System.IO;
using System.Text.Json;

namespace OrderWatch.Utils;

/// <summary>
/// 版本管理工具类
/// </summary>
public static class VersionManager
{
    private static readonly string VersionFilePath = GetVersionFilePath();
    private static string? _currentVersion;

    /// <summary>
    /// 获取版本文件路径，优先从项目根目录读取
    /// </summary>
    private static string GetVersionFilePath()
    {
        // 尝试多个可能的路径
        var possiblePaths = new[]
        {
            // 1. 项目根目录 (开发时)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "version.json"),
            // 2. 解决方案根目录 (开发时)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "version.json"),
            // 3. 运行目录 (发布时)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json"),
            // 4. 当前工作目录
            Path.Combine(Directory.GetCurrentDirectory(), "version.json"),
            // 5. 上级目录
            Path.Combine(Directory.GetCurrentDirectory(), "..", "version.json")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var normalizedPath = Path.GetFullPath(path);
                if (File.Exists(normalizedPath))
                {
                    Console.WriteLine($"✅ 找到版本文件: {normalizedPath}");
                    return normalizedPath;
                }
            }
            catch
            {
                // 忽略路径解析错误
            }
        }

        // 如果都没找到，返回默认路径（运行目录）
        var defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");
        Console.WriteLine($"⚠️ 使用默认版本文件路径: {defaultPath}");
        return defaultPath;
    }

    /// <summary>
    /// 获取当前版本号
    /// </summary>
    public static string GetCurrentVersion()
    {
        if (_currentVersion != null)
            return _currentVersion;

        try
        {
            if (File.Exists(VersionFilePath))
            {
                var jsonContent = File.ReadAllText(VersionFilePath);
                var versionData = JsonSerializer.Deserialize<VersionData>(jsonContent);
                _currentVersion = versionData?.Version ?? "0.01";
            }
            else
            {
                _currentVersion = "0.01";
                SaveVersion(_currentVersion);
            }
        }
        catch
        {
            _currentVersion = "0.01";
        }

        return _currentVersion;
    }

    /// <summary>
    /// 递增版本号并保存
    /// </summary>
    public static string IncrementVersion()
    {
        var currentVersion = GetCurrentVersion();
        
        if (decimal.TryParse(currentVersion, out var version))
        {
            version += 0.01m;
            var newVersion = version.ToString("F2");
            SaveVersion(newVersion);
            _currentVersion = newVersion;
            return newVersion;
        }

        return currentVersion;
    }

    /// <summary>
    /// 保存版本号到文件
    /// </summary>
    private static void SaveVersion(string version)
    {
        try
        {
            var versionData = new VersionData { Version = version };
            var jsonContent = JsonSerializer.Serialize(versionData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            var directory = Path.GetDirectoryName(VersionFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(VersionFilePath, jsonContent);
        }
        catch
        {
            // 静默处理保存失败
        }
    }

    /// <summary>
    /// 格式化版本显示
    /// </summary>
    public static string GetFormattedVersion()
    {
        var version = GetCurrentVersion();
        return $"币安合约交易系统 V{version}";
    }

    /// <summary>
    /// 版本数据模型
    /// </summary>
    private class VersionData
    {
        public string Version { get; set; } = "0.01";
        public string? lastUpdate { get; set; }
    }
}

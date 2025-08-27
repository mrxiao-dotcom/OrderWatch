using System;
using System.IO;
using System.Text.Json;
using System.Reflection;

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
            // 方法1: 尝试从文件读取
            if (File.Exists(VersionFilePath))
            {
                var jsonContent = File.ReadAllText(VersionFilePath);
                var versionData = JsonSerializer.Deserialize<VersionData>(jsonContent);
                _currentVersion = versionData?.Version ?? GetVersionFromAssembly();
                Console.WriteLine($"✅ 从文件读取版本号: {_currentVersion}");
            }
            // 方法2: 从内嵌资源读取（发布版本）
            else if (TryGetVersionFromEmbeddedResource(out var embeddedVersion))
            {
                _currentVersion = embeddedVersion;
                Console.WriteLine($"✅ 从内嵌资源读取版本号: {_currentVersion}");
            }
            // 方法3: 从程序集版本读取
            else
            {
                _currentVersion = GetVersionFromAssembly();
                Console.WriteLine($"✅ 从程序集读取版本号: {_currentVersion}");
                SaveVersion(_currentVersion);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 读取版本号失败: {ex.Message}");
            _currentVersion = GetVersionFromAssembly();
        }

        return _currentVersion;
    }

    /// <summary>
    /// 从内嵌资源读取版本号
    /// </summary>
    private static bool TryGetVersionFromEmbeddedResource(out string version)
    {
        version = "0.01";
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("version.json");
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var jsonContent = reader.ReadToEnd();
                var versionData = JsonSerializer.Deserialize<VersionData>(jsonContent);
                version = versionData?.Version ?? "0.01";
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 从内嵌资源读取版本失败: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// 从程序集版本信息读取版本号
    /// </summary>
    private static string GetVersionFromAssembly()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            if (version != null && version.Major > 0)
            {
                // 将程序集版本转换为我们的格式 (例如: 0.50.0.0 -> 0.50)
                var versionString = $"{version.Major}.{version.Minor:D2}";
                Console.WriteLine($"📊 程序集版本: {version} -> {versionString}");
                return versionString;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 读取程序集版本失败: {ex.Message}");
        }
        return "0.01";
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

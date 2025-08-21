using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderWatch.Models;

namespace OrderWatch.Services;

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly string _configDirectory;
    private readonly string _accountsConfigPath;
    private readonly string _candidateSymbolsConfigPath;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrderWatch");
        _accountsConfigPath = Path.Combine(_configDirectory, "accounts.json");
        _candidateSymbolsConfigPath = Path.Combine(_configDirectory, "candidate_symbols.json");

        // 确保配置目录存在
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            _logger.LogInformation("创建配置目录: {Directory}", _configDirectory);
        }
    }

    public string GetConfigDirectory() => _configDirectory;
    public string GetAccountsConfigPath() => _accountsConfigPath;
    public string GetCandidateSymbolsConfigPath() => _candidateSymbolsConfigPath;

    public async Task<List<AccountInfo>> LoadAccountsAsync()
    {
        try
        {
            if (!File.Exists(_accountsConfigPath))
            {
                _logger.LogInformation("账户配置文件不存在，返回空列表");
                return new List<AccountInfo>();
            }

            var json = await File.ReadAllTextAsync(_accountsConfigPath);
            var accounts = JsonSerializer.Deserialize<List<AccountInfo>>(json) ?? new List<AccountInfo>();
            _logger.LogInformation("成功加载 {Count} 个账户", accounts.Count);
            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载账户配置失败");
            return new List<AccountInfo>();
        }
    }

    public async Task SaveAccountAsync(AccountInfo account)
    {
        try
        {
            var accounts = await LoadAccountsAsync();
            var existingIndex = accounts.FindIndex(a => a.Name == account.Name);
            
            if (existingIndex >= 0)
            {
                accounts[existingIndex] = account;
                _logger.LogInformation("更新账户: {Name}", account.Name);
            }
            else
            {
                accounts.Add(account);
                _logger.LogInformation("添加新账户: {Name}", account.Name);
            }

            await SaveAccountsAsync(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存账户失败: {Name}", account.Name);
            throw;
        }
    }

    public async Task DeleteAccountAsync(string accountName)
    {
        try
        {
            var accounts = await LoadAccountsAsync();
            var removed = accounts.RemoveAll(a => a.Name == accountName);
            
            if (removed > 0)
            {
                await SaveAccountsAsync(accounts);
                _logger.LogInformation("删除账户: {Name}", accountName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除账户失败: {Name}", accountName);
            throw;
        }
    }

    private async Task SaveAccountsAsync(List<AccountInfo> accounts)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_accountsConfigPath, json);
            _logger.LogDebug("账户配置已保存到: {Path}", _accountsConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存账户配置到文件失败");
            throw;
        }
    }

    public async Task<List<CandidateSymbol>> LoadCandidateSymbolsAsync()
    {
        try
        {
            if (!File.Exists(_candidateSymbolsConfigPath))
            {
                var defaultSymbols = GetDefaultCandidateSymbols();
                await SaveCandidateSymbolsAsync(defaultSymbols);
                _logger.LogInformation("创建默认候选币列表");
                return defaultSymbols;
            }

            var json = await File.ReadAllTextAsync(_candidateSymbolsConfigPath);
            var symbols = JsonSerializer.Deserialize<List<CandidateSymbol>>(json) ?? new List<CandidateSymbol>();
            _logger.LogInformation("成功加载 {Count} 个候选币", symbols.Count);
            return symbols;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载候选币配置失败");
            return GetDefaultCandidateSymbols();
        }
    }

    public async Task SaveCandidateSymbolsAsync(List<CandidateSymbol> symbols)
    {
        try
        {
            var json = JsonSerializer.Serialize(symbols, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_candidateSymbolsConfigPath, json);
            _logger.LogDebug("候选币配置已保存到: {Path}", _candidateSymbolsConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存候选币配置失败");
            throw;
        }
    }

    public async Task AddCandidateSymbolAsync(string symbol)
    {
        try
        {
            var symbols = await LoadCandidateSymbolsAsync();
            if (symbols.Any(s => s.Symbol == symbol))
            {
                _logger.LogWarning("候选币已存在: {Symbol}", symbol);
                return;
            }

            var newSymbol = new CandidateSymbol
            {
                Symbol = symbol,
                LatestPrice = 0,
                PriceChangePercent = 0,
                LastUpdateTime = DateTime.Now,
                IsSelected = false
            };

            symbols.Add(newSymbol);
            await SaveCandidateSymbolsAsync(symbols);
            _logger.LogInformation("添加候选币: {Symbol}", symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加候选币失败: {Symbol}", symbol);
            throw;
        }
    }

    public async Task RemoveCandidateSymbolAsync(string symbol)
    {
        try
        {
            var symbols = await LoadCandidateSymbolsAsync();
            var removed = symbols.RemoveAll(s => s.Symbol == symbol);
            
            if (removed > 0)
            {
                await SaveCandidateSymbolsAsync(symbols);
                _logger.LogInformation("删除候选币: {Symbol}", symbol);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除候选币失败: {Symbol}", symbol);
            throw;
        }
    }

    private List<CandidateSymbol> GetDefaultCandidateSymbols()
    {
        return new List<CandidateSymbol>
        {
            new() { Symbol = "BTCUSDT", LatestPrice = 0, PriceChangePercent = 0, LastUpdateTime = DateTime.Now, IsSelected = false },
            new() { Symbol = "ETHUSDT", LatestPrice = 0, PriceChangePercent = 0, LastUpdateTime = DateTime.Now, IsSelected = false },
            new() { Symbol = "BNBUSDT", LatestPrice = 0, PriceChangePercent = 0, LastUpdateTime = DateTime.Now, IsSelected = false },
            new() { Symbol = "ADAUSDT", LatestPrice = 0, PriceChangePercent = 0, LastUpdateTime = DateTime.Now, IsSelected = false },
            new() { Symbol = "SOLUSDT", LatestPrice = 0, PriceChangePercent = 0, LastUpdateTime = DateTime.Now, IsSelected = false }
        };
    }
}

using System.Text.Json;
using OrderWatch.Models;
using System.IO;

namespace OrderWatch.Services;

public class ConfigService : IConfigService
{
    private readonly string _configDirectory;
    private readonly string _accountsConfigPath;
    private readonly string _candidateSymbolsConfigPath;

    public ConfigService()
    {
        _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrderWatch");
        _accountsConfigPath = Path.Combine(_configDirectory, "accounts.json");
        _candidateSymbolsConfigPath = Path.Combine(_configDirectory, "candidate_symbols.json");

        // 确保配置目录存在
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
            // Console.WriteLine($"创建配置目录: {_configDirectory}");
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
                // Console.WriteLine("账户配置文件不存在，返回空列表");
                return new List<AccountInfo>();
            }

            var json = await File.ReadAllTextAsync(_accountsConfigPath);
            var accounts = JsonSerializer.Deserialize<List<AccountInfo>>(json) ?? new List<AccountInfo>();
            // Console.WriteLine($"成功加载 {accounts.Count} 个账户");
            return accounts;
        }
        catch (Exception)
        {
            // Console.WriteLine($"加载账户配置失败");
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
                // Console.WriteLine($"更新账户: {account.Name}");
            }
            else
            {
                accounts.Add(account);
                // Console.WriteLine($"添加新账户: {account.Name}");
            }

            await SaveAccountsAsync(accounts);
        }
        catch (Exception)
        {
            // Console.WriteLine($"保存账户失败: {account.Name}");
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
                // Console.WriteLine($"删除账户: {accountName}");
            }
        }
        catch (Exception)
        {
            // Console.WriteLine($"删除账户失败: {accountName}");
            throw;
        }
    }

    private async Task SaveAccountsAsync(List<AccountInfo> accounts)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_accountsConfigPath, json);
            // Console.WriteLine($"账户配置已保存到: {_accountsConfigPath}");
        }
        catch (Exception)
        {
            // Console.WriteLine($"保存账户配置到文件失败");
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
                // Console.WriteLine("创建默认候选币列表");
                return defaultSymbols;
            }

            var json = await File.ReadAllTextAsync(_candidateSymbolsConfigPath);
            var symbols = JsonSerializer.Deserialize<List<CandidateSymbol>>(json) ?? new List<CandidateSymbol>();
            // Console.WriteLine($"成功加载 {symbols.Count} 个候选币");
            return symbols;
        }
        catch (Exception)
        {
            // Console.WriteLine($"加载候选币配置失败");
            return GetDefaultCandidateSymbols();
        }
    }

    public async Task SaveCandidateSymbolsAsync(List<CandidateSymbol> symbols)
    {
        try
        {
            var json = JsonSerializer.Serialize(symbols, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_candidateSymbolsConfigPath, json);
            // Console.WriteLine($"候选币配置已保存到: {_candidateSymbolsConfigPath}");
        }
        catch (Exception)
        {
            // Console.WriteLine($"保存候选币配置失败");
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
                // Console.WriteLine($"候选币已存在: {symbol}");
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
            // Console.WriteLine($"添加候选币: {symbol}");
        }
        catch (Exception)
        {
            // Console.WriteLine($"添加候选币失败: {symbol}");
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
                // Console.WriteLine($"删除候选币: {symbol}");
            }
        }
        catch (Exception)
        {
            // Console.WriteLine($"删除候选币失败: {symbol}");
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

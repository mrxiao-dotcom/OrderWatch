using OrderWatch.Models;

namespace OrderWatch.Services;

public interface IConfigService
{
    // 账户配置管理
    Task<List<AccountInfo>> LoadAccountsAsync();
    Task SaveAccountAsync(AccountInfo account);
    Task DeleteAccountAsync(string accountName);
    
    // 候选币管理
    Task<List<CandidateSymbol>> LoadCandidateSymbolsAsync();
    Task SaveCandidateSymbolsAsync(List<CandidateSymbol> symbols);
    Task AddCandidateSymbolAsync(string symbol);
    Task RemoveCandidateSymbolAsync(string symbol);
    
    // 配置路径管理
    string GetConfigDirectory();
    string GetAccountsConfigPath();
    string GetCandidateSymbolsConfigPath();
}

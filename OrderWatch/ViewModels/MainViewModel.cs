using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using OrderWatch.Models;

namespace OrderWatch.ViewModels
{
    /// <summary>
    /// 临时的MainViewModel，用于解决编译问题
    /// 实际功能已迁移到TestViewModel
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        public string WindowTitle { get; set; } = "OrderWatch - 币安期货下单系统";
        
        public ObservableCollection<AccountInfo> Accounts { get; } = new();
        public ObservableCollection<PositionInfo> Positions { get; } = new();
        public ObservableCollection<OrderInfo> Orders { get; } = new();
        public ObservableCollection<ConditionalOrder> ConditionalOrders { get; } = new();
        public ObservableCollection<CandidateSymbol> CandidateSymbols { get; } = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "使用TestViewModel";

        // 为了兼容性提供的空属性
        public string NewSymbolInput { get; set; } = "";
        public ObservableCollection<string> SymbolSuggestions { get; } = new();

        // 为了兼容性，提供空的Dispose方法
        [Obsolete("请直接调用Dispose()方法")]
        public void ForceExit()
        {
            // 空实现
        }

        public void Dispose()
        {
            // 空实现
        }

        public async Task StartServicesAsync()
        {
            await Task.CompletedTask;
        }

        // 为了兼容性提供的空方法实现
        public async Task AutoFillSymbolToOrderAreasAsync(string symbol)
        {
            await Task.CompletedTask;
        }

        public async Task PasteSymbolAsync()
        {
            await Task.CompletedTask;
        }

        public async Task RemoveCandidateSymbolAsync()
        {
            await Task.CompletedTask;
        }

        public async Task AddSymbolFromInputAsync()
        {
            await Task.CompletedTask;
        }

        public async Task AutoFillPositionToOrderAreasAsync(PositionInfo position)
        {
            await Task.CompletedTask;
        }
    }
} 
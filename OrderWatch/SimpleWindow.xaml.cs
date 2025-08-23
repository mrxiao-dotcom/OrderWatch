using System;
using System.Windows;

namespace OrderWatch
{
    public partial class SimpleWindow : Window
    {
        public SimpleWindow()
        {
            Console.WriteLine("SimpleWindow构造函数开始");
            InitializeComponent();
            Console.WriteLine("SimpleWindow构造函数完成");
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("按钮点击成功！WPF事件系统正常工作。", "测试成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

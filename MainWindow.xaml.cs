using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace EnvVarManager;

public partial class MainWindow : Window
{
    private readonly EnvironmentService _environmentService = new();
    private readonly ObservableCollection<EnvVarEntry> _items = new();

    public MainWindow()
    {
        InitializeComponent();
        EnvDataGrid.ItemsSource = _items;
        LoadVariables();
    }

    private EnvScope CurrentScope => GetSelectedScope();

    private EnvScope GetSelectedScope()
    {
        if (ScopeComboBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag)
        {
            return tag == "System" ? EnvScope.System : EnvScope.User;
        }

        return EnvScope.User;
    }

    private void LoadVariables()
    {
        try
        {
            _items.Clear();
            foreach (var entry in _environmentService.GetVariables(CurrentScope))
            {
                _items.Add(entry);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"读取环境变量失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadVariables();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadVariables();
    }

    private EnvVarEntry? GetSelectedEntry()
    {
        return EnvDataGrid.SelectedItem as EnvVarEntry;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditEnvVarWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _environmentService.SetVariable(dialog.VariableName, dialog.VariableValue, CurrentScope);
                LoadVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"新增环境变量失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedEntry();
        if (selected is null)
        {
            MessageBox.Show(this, "请先选择一项进行编辑。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new EditEnvVarWindow(selected.Name, selected.Value)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _environmentService.SetVariable(dialog.VariableName, dialog.VariableValue, CurrentScope);
                LoadVariables();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"编辑环境变量失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedEntry();
        if (selected is null)
        {
            MessageBox.Show(this, "请先选择一项进行删除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"确定要删除环境变量 \"{selected.Name}\" 吗？此操作不可撤销。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _environmentService.DeleteVariable(selected.Name, CurrentScope);
            LoadVariables();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"删除环境变量失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "备份环境变量到文件",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
            FileName = $"env_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            try
            {
                _environmentService.BackupAll(dialog.FileName);
                MessageBox.Show(this, "备份完成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"备份失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "从备份文件恢复环境变量",
            Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            var result = MessageBox.Show(
                this,
                "将从备份文件覆盖当前用户和系统环境变量，是否继续？建议事先做好额外备份。",
                "确认恢复",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                _environmentService.RestoreFromBackup(dialog.FileName);
                LoadVariables();
                MessageBox.Show(this, "恢复完成。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"恢复失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}



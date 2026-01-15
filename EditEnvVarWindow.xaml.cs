using System.Windows;

namespace EnvVarManager;

public partial class EditEnvVarWindow : Window
{
    public string VariableName => NameTextBox.Text.Trim();
    public string VariableValue => ValueTextBox.Text;

    public EditEnvVarWindow()
    {
        InitializeComponent();
    }

    public EditEnvVarWindow(string name, string value) : this()
    {
        NameTextBox.Text = name;
        NameTextBox.IsEnabled = false; // 编辑时不允许修改名称，避免误删
        ValueTextBox.Text = value;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(VariableName))
        {
            MessageBox.Show(this, "变量名不能为空。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }
}



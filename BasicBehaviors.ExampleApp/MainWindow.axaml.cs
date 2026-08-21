using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System.Diagnostics;

namespace BasicBehaviors.ExampleApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void OnValueChange()
        {
            myTextBlock2.Text = $"Value changed to: {mySlider2.Value}";
        }

        public void OnValueChange(object sender, RangeBaseValueChangedEventArgs e)
        {
            Debug.WriteLine($"Value changed to: {e.NewValue}");
        }
    }
}
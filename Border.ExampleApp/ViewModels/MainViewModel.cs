using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Border.ExampleApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IBorderSettingService _borderSettingService;
        private readonly IClipboardService _clipboardService;
        public MainViewModel(IDialogService dialogService, IBorderSettingService borderSettingService, IClipboardService clipboardService)
        {
            _borderSetting = new BorderSettingViewModel();
            _borderSettings = new ObservableCollection<BorderSettingViewModel>();
            _dialogService = dialogService;
            _borderSettingService = borderSettingService;
            _clipboardService = clipboardService;
        }

        public MainViewModel() : this(null!, null!, null!)
        {
            if (!Design.IsDesignMode) throw new InvalidOperationException("MainViewModel requires IDialogService, IBorderSettingService, and IClipboardService.");
            for(int i = 0; i < 10; i++)
            {
                _borderSettings.Add(new BorderSettingViewModel()
                {
                    Name = $"Design Test {i + 1}",
                });
            }

        }

        [ObservableProperty]
        private string _saveFileName = "borderSettings.json";

        [ObservableProperty]
        private BorderSettingViewModel _borderSetting;

        [ObservableProperty]
        private ObservableCollection<BorderSettingViewModel> _borderSettings;

        [ObservableProperty]
        private BorderSettingViewModel _selectedBorderSetting = null!;

        partial void OnSelectedBorderSettingChanged(BorderSettingViewModel value)
        {
            if(value is not null)
            {
                BorderSetting.CopyFrom(value);
            }
        }


        [RelayCommand]
        private async Task SaveBorderSetting()
        {
            if (_dialogService is null) return;

            var result = await _dialogService.ShowInputPromptDialog("Input Save Name:", "Input", BorderSetting.Name ?? string.Empty);
            if (result is not null)
            {
                var firstItem = BorderSettings.FirstOrDefault(item => item.Name == result);
                if(firstItem is not null)
                {
                    firstItem.CopyFrom(BorderSetting);
                }
                else
                {
                    firstItem = new BorderSettingViewModel(BorderSetting.GetBorderSetting());
                    BorderSettings.Add(firstItem);
                }
                firstItem.Name = result;
                await _borderSettingService.SaveAsync(SaveFileName, BorderSettings.Select(item => item.GetBorderSetting()));
            }
            else
            {
                await _dialogService.ShowWarningDialog("No input provided.", "Warning");
            }

        }

        [RelayCommand]
        private async Task LoadBorderSetting()
        {
            BorderSettings.Clear();
            await _borderSettingService.LoadAsync(SaveFileName).ContinueWith(task =>
            {
                if (task.Exception is not null)
                {
                    _dialogService?.ShowErrorDialog($"Error loading settings: {task.Exception.Message}", "Error");
                    return;
                }
                var settings = task.Result;
                BorderSettings.Clear();
                foreach (var setting in settings)
                {
                    BorderSettings.Add(new BorderSettingViewModel(setting));
                }
            });
            

        }

        [RelayCommand]
        private async Task GenCodeAxaml()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Margin=\"{BorderSetting.Margin}\"");
            sb.AppendLine($"Padding=\"{BorderSetting.Padding}\"");
            sb.AppendLine($"Thickness=\"{BorderSetting.Thickness}\"");
            sb.AppendLine($"CornerRadius=\"{BorderSetting.CornerRadius}\"");
            sb.AppendLine($"BorderColor=\"{BorderSetting.BorderColor}\"");
            sb.AppendLine($"BackgroundColor=\"{BorderSetting.BackgroundColor}\"");
            sb.AppendLine($"BoxShadow=\"{BorderSetting.BoxShadow}\"");
            await _clipboardService.SetTextAsync(sb.ToString());
            await _dialogService.ShowInfoDialog("Copied to clipboard" + Environment.NewLine + sb.ToString(), "Axaml Code");
        }

    }



    public partial class BorderSettingViewModel: ViewModelBase
    {
        public BorderSettingViewModel()
        {

        }

        public BorderSettingViewModel(BorderSetting setting)
        {
            _name = setting.Name;
            _nameTextColor = Color.Parse(setting.NameTextColor);
            _margin = setting.Margin;
            _padding = setting.Padding;
            _thickness = setting.Thickness;
            _cornerRadius = setting.CornerRadius;
            _borderColor = Color.Parse(setting.BorderColor);
            _backgroundColor = Color.Parse(setting.BackgroundColor);
            _boxShadow = setting.BoxShadow;
        }

        public void CopyFrom(BorderSettingViewModel other)
        {
            Name = other.Name;
            NameTextColor = other.NameTextColor;
            Margin = other.Margin;
            Padding = other.Padding;
            Thickness = other.Thickness;
            CornerRadius = other.CornerRadius;
            BorderColor = other.BorderColor;
            BackgroundColor = other.BackgroundColor;
            BoxShadow = other.BoxShadow;
        }

        public BorderSetting GetBorderSetting()
        {
            return new BorderSetting
            {
                Name = Name,
                NameTextColor = NameTextColor.ToString(),
                Margin = Margin,
                Padding = Padding,
                Thickness = Thickness,
                CornerRadius = CornerRadius,
                BorderColor = BorderColor.ToString(),
                BackgroundColor = BackgroundColor.ToString(),
                BoxShadow = BoxShadow
            };
        }


        [ObservableProperty]
        private string _name = "Default";

        [ObservableProperty]
        private Color _nameTextColor = Color.Parse("Black");

        [ObservableProperty]
        private string _margin = "12";

        [ObservableProperty]
        private string _padding = "12";

        [ObservableProperty]
        private decimal _thickness = 5;

        [ObservableProperty]
        private string _cornerRadius = "00 00";

        [ObservableProperty]
        private Color _borderColor = Color.Parse("Orange");

        [ObservableProperty]
        private Color _backgroundColor = Color.Parse("#00FF00");

        [ObservableProperty]
        private string _boxShadow = "10 5 10 0 DarkOrange";
    }
}

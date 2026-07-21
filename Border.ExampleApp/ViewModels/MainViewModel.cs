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

                    firstItem = BorderSetting.Clone();
                    BorderSettings.Add(firstItem);
                }
                firstItem.Name = result;
                await _borderSettingService.SaveAsync(SaveFileName, BorderSettings.Select(item => item.ToBorderSetting()));
            }
            else
            {
                await _dialogService.ShowWarningDialog("No input provided.", "Warning");
            }

        }

        [RelayCommand]
        private async Task LoadBorderSetting()
        {
            TaskScheduler uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            
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
                    BorderSettings.Add(setting.ToViewModel());
                }
            }, uiScheduler);

        }

        [RelayCommand]
        private async Task CopyCodeAxaml()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Margin=\"{BorderSetting.Margin}\"");
            sb.AppendLine($"Padding=\"{BorderSetting.Padding}\"");
            sb.AppendLine($"BorderThickness=\"{BorderSetting.Thickness}\"");
            sb.AppendLine($"CornerRadius=\"{BorderSetting.CornerRadius}\"");
            sb.AppendLine($"BorderBrush=\"{BorderSetting.BorderColor}\"");
            sb.AppendLine($"Background=\"{BorderSetting.BackgroundColor}\"");
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

    public static class BorderSettingViewModelExtension
    {
        public static BorderSettingViewModel CopyFrom(this BorderSettingViewModel source, BorderSettingViewModel other)
        {
            source.Name = other.Name;
            source.NameTextColor = other.NameTextColor;
            source.Margin = other.Margin;
            source.Padding = other.Padding;
            source.Thickness = other.Thickness;
            source.CornerRadius = other.CornerRadius;
            source.BorderColor = other.BorderColor;
            source.BackgroundColor = other.BackgroundColor;
            source.BoxShadow = other.BoxShadow;
            return source;
        }

        public static BorderSettingViewModel CopyFrom(this BorderSettingViewModel source, BorderSetting other)
        {
            source.Name = other.Name;
            source.NameTextColor = DeserializeColor(other.NameTextColor);
            source.Margin = other.Margin;
            source.Padding = other.Padding;
            source.Thickness = other.Thickness;
            source.CornerRadius = other.CornerRadius;
            source.BorderColor = DeserializeColor(other.BorderColor);
            source.BackgroundColor = DeserializeColor(other.BackgroundColor);
            source.BoxShadow = other.BoxShadow;
            return source;
        }

        public static BorderSettingViewModel Clone(this BorderSettingViewModel source)
        {
            return new BorderSettingViewModel
            {
                Name = source.Name,
                NameTextColor = source.NameTextColor,
                Margin = source.Margin,
                Padding = source.Padding,
                Thickness = source.Thickness,
                CornerRadius = source.CornerRadius,
                BorderColor = source.BorderColor,
                BackgroundColor = source.BackgroundColor,
                BoxShadow = source.BoxShadow
            };
        }

        public static BorderSetting ToBorderSetting(this BorderSettingViewModel source)
        {
            return new BorderSetting
            {
                Name = source.Name,
                NameTextColor = SerializeColor(source.NameTextColor),
                Margin = source.Margin,
                Padding = source.Padding,
                Thickness = source.Thickness,
                CornerRadius = source.CornerRadius,
                BorderColor = SerializeColor(source.BorderColor),
                BackgroundColor = SerializeColor(source.BackgroundColor),
                BoxShadow = source.BoxShadow
            };
        }

        public static BorderSettingViewModel ToViewModel(this BorderSetting source)
        {
            return new BorderSettingViewModel
            {
                Name = source.Name,
                NameTextColor = DeserializeColor(source.NameTextColor),
                Margin = source.Margin,
                Padding = source.Padding,
                Thickness = source.Thickness,
                CornerRadius = source.CornerRadius,
                BorderColor = DeserializeColor(source.BorderColor),
                BackgroundColor = DeserializeColor(source.BackgroundColor),
                BoxShadow = source.BoxShadow
            };
        }

        private static Color DeserializeColor(string colorString)
        {
            if(Color.TryParse(colorString, out Color color))
            {
                return color;
            }

            return Color.Parse("Black");
        }

        private static string SerializeColor(Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}

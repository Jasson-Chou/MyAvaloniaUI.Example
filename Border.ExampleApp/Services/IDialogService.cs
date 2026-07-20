using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Services
{
    public interface IDialogService
    {
        Task ShowInfoDialog(string message, string title);
        Task ShowWarningDialog(string message, string title);
        Task ShowErrorDialog(string message, string title);
        Task<string?> ShowInputPromptDialog(string promptText, string title, string defaultValue = "");
    }

    public class DefaultDialogService : IDialogService
    {
        private readonly Func<Window?> _getWindow;

        public DefaultDialogService(Func<Window?> getWindow)
        {
            _getWindow = getWindow;
        }

        public async Task ShowInfoDialog(string message, string title)
        {
            await ShowDialog(message, title, Icon.Info);
        }

        public async Task ShowWarningDialog(string message, string title)
        {
            await ShowDialog(message, title, Icon.Warning);
        }

        public async Task ShowErrorDialog(string message, string title)
        {
            await ShowDialog(message, title, Icon.Error);
        }

        public async Task ShowDialog(string message, string title, Icon icon)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentTitle = title,
                        ContentMessage = message,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Icon = icon,
                        ButtonDefinitions = ButtonEnum.Ok,
                    });
            var window = _getWindow();
            if(window is not null)
            {
                await box.ShowWindowDialogAsync(window);
            }
        }

        public async Task<string?> ShowInputPromptDialog(string promptText, string title, string defaultValue = "")
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                    new MessageBoxStandardParams
                    {
                        ContentTitle = title,
                        ContentMessage = promptText,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        ButtonDefinitions = ButtonEnum.OkCancel,
                        InputParams = new InputParams
                        {
                            DefaultValue = defaultValue,
                            Multiline = false,
                        },
                    });
            var window = _getWindow();
            if (window is not null)
            {
                ButtonResult result = await box.ShowWindowDialogAsync(window);

                if (result == ButtonResult.Ok)
                {
                    string userInput = box.InputValue; // 取得輸入字串
                    return userInput;
                }
            }

            return null;
        }
    }
}

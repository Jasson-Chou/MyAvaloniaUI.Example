using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Services
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
        Task<string?> GetTextAsync();
    }

    public class ClipboardService : IClipboardService
    {
        private readonly Func<Window?> _getWindow;
        public ClipboardService(Func<Window?> getWindow)
        {
            _getWindow = getWindow;
        }

        private IClipboard Clipboard =>
            _getWindow()?.Clipboard ?? throw new InvalidOperationException("Clipboard is not available.");

        public Task SetTextAsync(string text) => Clipboard.SetTextAsync(text);
        public Task<string?> GetTextAsync() => Clipboard.TryGetTextAsync();
    }
}

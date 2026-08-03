using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaBasicDrawing.ExampleApp.Models
{
    public interface IUiTimer
    {

        TimeSpan Interval { get; set; }

        bool IsEnabled { get; }

        void Start();
        void Stop();

        event EventHandler? Tick;
    }

    public class DefaultUiTimer : IUiTimer
    {

        public DefaultUiTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }

        private readonly DispatcherTimer _timer;

        public TimeSpan Interval { get => _timer.Interval; set => _timer.Interval = value; }

        public bool IsEnabled => _timer.IsEnabled;

        public event EventHandler? Tick;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}

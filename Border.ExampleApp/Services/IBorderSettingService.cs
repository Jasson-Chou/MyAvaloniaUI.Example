using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Border.ExampleApp.Services
{
    public class BorderSetting
    {
        public string Name { get; set; } = string.Empty;

        public string NameTextColor { get; set; } = string.Empty;

        public string Margin { get; set; } = string.Empty;

        public string Padding { get; set; } = string.Empty;

        public decimal Thickness { get; set; }

        public string CornerRadius { get; set; } = string.Empty;

        public string BoxShadow { get; set; } = string.Empty;

        public string BorderColor { get; set; } = string.Empty;

        public string BackgroundColor { get; set; } = string.Empty;
    }

    public interface IBorderSettingService
    {
        Task<IEnumerable<BorderSetting>> LoadAsync(string fileName);

        Task SaveAsync(string fileName, IEnumerable<BorderSetting> settings);
    }
}

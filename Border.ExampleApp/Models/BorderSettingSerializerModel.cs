using Border.ExampleApp.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Border.ExampleApp.Models
{
    public class BorderSettingSerializerModel : IBorderSettingService
    {
        public async Task<IEnumerable<BorderSetting>> LoadAsync(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            return (await JsonSerializer.DeserializeAsync<List<BorderSetting>>(stream)) ?? new List<BorderSetting>();
        }

        public async Task SaveAsync(string fileName, IEnumerable<BorderSetting> settings)
        {
            using var stream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(stream, settings);
        }
    }
}

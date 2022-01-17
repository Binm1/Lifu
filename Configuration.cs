using Dalamud.Configuration;

namespace Lifu
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool undiyici;
        public void Initialize() { }


        public void Save() => DalamudApi.PluginInterface.SavePluginConfig(this);
    }
}

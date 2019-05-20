using Newtonsoft.Json;

namespace BattletechModUtilities
{
    public class DefaultSettings : IDebug
    {
        [JsonConstructor]
        public DefaultSettings()
        {
        }

        public DefaultSettings(bool? debug)
        {
            Debug = debug ?? false;
        }

        public readonly bool Debug = false;

        public bool DebugOn => Debug;
    }
}

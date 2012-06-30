using Chutzpah.VS.Common.Settings;

namespace Chutzpah.VS2012.TestAdapter
{
    public interface IChutzpahSettingsMapper
    {
        void MapSettings(ChutzpahSettings settings);
        ChutzpahAdapterSettings Settings { get; }
    }
}
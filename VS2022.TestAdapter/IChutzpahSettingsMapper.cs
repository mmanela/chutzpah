using Chutzpah.VS.Common.Settings;

namespace Chutzpah.VS2022.TestAdapter
{
    public interface IChutzpahSettingsMapper
    {
        void MapSettings(ChutzpahUTESettings settings);
        ChutzpahAdapterSettings Settings { get; }
    }
}
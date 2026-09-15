using CatGame.Core.Data;

namespace CatGame.Core.Interfaces
{
    public interface ISettingsService
    {
        public void Save(SettingsData data);
        public SettingsData Load();
    }
}

using CatGame.Core.Data;

namespace CatGame.Core.Interfaces
{
    public interface ISaveSettingsService
    {
        public bool HasSave();
        public void Save(SettingsData data);
        public SettingsData Load();
    }
}

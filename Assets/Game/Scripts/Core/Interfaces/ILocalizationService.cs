using System;

namespace CatGame.Core.Interfaces
{
    public interface ILocalizationService
    {
        public event Action OnLocalizationChanged;
        public string CurrentLanguage { get; }
        public void ChangeLanguage(string language);
        public string GetTextByID(string id);
    }
}
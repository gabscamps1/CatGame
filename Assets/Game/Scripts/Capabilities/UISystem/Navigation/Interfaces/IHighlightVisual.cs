using CatGame.Core.Data;

namespace CatGame.Capabilities.UISystem
{
    public interface IHighlightVisual 
    {
        public void AttachTo(NavigableElement element, PlayerId playerId);
        public void Disattach(PlayerId playerId);
        public void Refresh();
    }
}

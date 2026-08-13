using CatGame.Core.Enums;

namespace CatGame.Capabilities.UISystem
{
    public interface IHighlighVisual 
    {
        public void AttachTo(NavigableElement element, PlayerId playerId);
    }
}

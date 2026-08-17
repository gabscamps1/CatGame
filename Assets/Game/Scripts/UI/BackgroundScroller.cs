using UnityEngine;
using UnityEngine.UIElements;

public class InfiniteBackground : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string elementName = "InfiniteBackground";

    [Header("Settings")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(50f, 0f); // X and Y speed in pixels per second

    private VisualElement _bgElement;
    private Vector2 _currentOffset;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        // Query the element from the UXML template
        _bgElement = uiDocument.rootVisualElement.Q<VisualElement>(elementName);
    }

    void Update()
    {
        if (_bgElement == null) return;

        // Calculate the new texture position offset based on frame time
        _currentOffset += scrollSpeed * Time.deltaTime;

        // Apply style updates directly to the VisualElement
        _bgElement.style.backgroundPositionX = new StyleBackgroundPosition(
            new BackgroundPosition(BackgroundPositionKeyword.Left, _currentOffset.x)
        );

        _bgElement.style.backgroundPositionY = new StyleBackgroundPosition(
            new BackgroundPosition(BackgroundPositionKeyword.Top, _currentOffset.y)
        );
    }
}
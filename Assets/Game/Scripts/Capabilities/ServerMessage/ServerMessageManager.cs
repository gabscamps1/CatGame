using TMPro;
using UnityEngine;

public class ServerMessageManager : MonoBehaviour
{
    public static ServerMessageManager Instance;

    [SerializeField] private GameObject serverMessageGameObject;
    [SerializeField] private TextMeshProUGUI serverMessageText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void ShowMessage(string text)
    {
        serverMessageText.text = text;
        serverMessageGameObject.SetActive(true);
    }

    public void HideMessage()
    {
        serverMessageGameObject.SetActive(false);
    }
}

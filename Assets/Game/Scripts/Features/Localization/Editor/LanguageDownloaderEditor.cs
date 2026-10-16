using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class LanguageDownloaderEditor : EditorWindow
{
    private readonly string url = "https://script.google.com/macros/s/AKfycbzCa1Rc3akK-bWReppJh-A1s_8LEz3PiBrbPILj2VlwSSUWIRCNhNxqpxre4t8EHXo-Sg/exec?sheet=";
    private readonly string pt = "Portuguese";
    private readonly string en = "English";

    [MenuItem("Tools/Language Downloader")]
    public static void ShowWindow()
    {
        GetWindow<LanguageDownloaderEditor>("Language Downloader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Baixar JSON de idiomas", EditorStyles.boldLabel);

        if (GUILayout.Button("Download JSON to Portuguese"))
        {
            DownloadJson(pt, "pt.json");
        }

        if (GUILayout.Button("Download JSON to English"))
        {
            DownloadJson(en, "en.json");
        }
    }

    private void DownloadJson(string language, string fileName)
    {
        // Cria o request
        using (UnityWebRequest www = UnityWebRequest.Get(url + language))
        {
            var operation = www.SendWebRequest();

            // Espera terminar (ok no Editor)
            while (!operation.isDone) { }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao baixar JSON: " + www.error);
                return;
            }

            // Caminho para StreamingAssets
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Languages", fileName);

            // Garante que a pasta existe
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Salva o arquivo
            File.WriteAllText(path, www.downloadHandler.text);
            Debug.Log("JSON baixado e salvo em: " + path);

            // Atualiza o Unity para reconhecer o novo arquivo
            AssetDatabase.Refresh();
        }
    }
}

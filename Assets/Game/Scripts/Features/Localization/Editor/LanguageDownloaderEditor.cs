using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class LanguageDownloaderEditor : EditorWindow
{
    private readonly string url = "https://script.google.com/macros/s/AKfycbxc8B2rmEbq5JkyLz-9U7Ptq1b0BhyJJg8O9DuzSLcLK_ssbHT5HHqOqoN6Z0nTvK0/exec?sheet=";
    private readonly string pt = "Portuguese";
    private readonly string en = "English";
    private readonly string jp = "Japanese";
    private readonly string es = "Spanish";
    private readonly string cn = "Chinese";
    private readonly string kr = "Korean";

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

        if (GUILayout.Button("Download JSON to Japanese"))
        {
            DownloadJson(jp, "jp.json");
        }

         if (GUILayout.Button("Download JSON to Spanish"))
        {
            DownloadJson(es, "es.json");
        }

         if (GUILayout.Button("Download JSON to Chinese"))
        {
            DownloadJson(cn, "cn.json");
        }

         if (GUILayout.Button("Download JSON to Korean"))
        {
            DownloadJson(kr, "kr.json");
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

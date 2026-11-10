using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class LanguageDownloaderEditor : EditorWindow
{
    private readonly string url = "https://script.google.com/macros/s/AKfycbxc8B2rmEbq5JkyLz-9U7Ptq1b0BhyJJg8O9DuzSLcLK_ssbHT5HHqOqoN6Z0nTvK0/exec?sheet=";

    private struct LanguageEntry
    {
        public string label;     // Texto do botão
        public string sheet;     // Nome da aba na planilha
        public string fileName;  // Nome do arquivo salvo

        public LanguageEntry(string label, string sheet, string fileName)
        {
            this.label = label;
            this.sheet = sheet;
            this.fileName = fileName;
        }
    }

    private readonly LanguageEntry[] languages =
    {
        new LanguageEntry("Portuguese", "Portuguese", "pt.json"),
        new LanguageEntry("English",    "English",    "en.json"),
        new LanguageEntry("Japanese",   "Japanese",   "jp.json"),
        new LanguageEntry("Spanish",    "Spanish",    "es.json"),
        new LanguageEntry("Chinese",    "Chinese",    "cn.json"),
        new LanguageEntry("Korean",     "Korean",     "kr.json"),
    };

    [MenuItem("Tools/Language Downloader")]
    public static void ShowWindow()
    {
        GetWindow<LanguageDownloaderEditor>("Language Downloader");
    }

    private void OnGUI()
    {
        GUILayout.Label("Baixar JSON de idiomas", EditorStyles.boldLabel);

        // Botão para baixar tudo de uma vez
        if (GUILayout.Button("Download ALL Languages", GUILayout.Height(30)))
        {
            DownloadAll();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Individual", EditorStyles.boldLabel);

        foreach (var lang in languages)
        {
            if (GUILayout.Button("Download JSON to " + lang.label))
            {
                if (DownloadJson(lang.sheet, lang.fileName))
                {
                    AssetDatabase.Refresh();
                }
            }
        }
    }

    private void DownloadAll()
    {
        int success = 0;
        int total = languages.Length;

        try
        {
            for (int i = 0; i < total; i++)
            {
                var lang = languages[i];

                EditorUtility.DisplayProgressBar(
                    "Language Downloader",
                    $"Baixando {lang.label} ({i + 1}/{total})...",
                    (float)i / total);

                if (DownloadJson(lang.sheet, lang.fileName))
                {
                    success++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Refresh uma única vez no final
        AssetDatabase.Refresh();

        if (success == total)
            Debug.Log($"Todos os idiomas foram baixados com sucesso ({success}/{total}).");
        else
            Debug.LogWarning($"Download concluído com falhas: {success}/{total} idiomas baixados. Veja os erros no Console.");
    }

    private bool DownloadJson(string sheet, string fileName)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url + sheet))
        {
            var operation = www.SendWebRequest();

            // Espera terminar (ok no Editor)
            while (!operation.isDone) { }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Erro ao baixar JSON ({sheet}): {www.error}");
                return false;
            }

            // Caminho para StreamingAssets
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Languages", fileName);

            // Garante que a pasta existe
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            // Salva o arquivo
            File.WriteAllText(path, www.downloadHandler.text);
            Debug.Log("JSON baixado e salvo em: " + path);

            return true;
        }
    }
}
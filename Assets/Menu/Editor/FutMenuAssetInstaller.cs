using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Instalador temporal: extrae FUT27_assets.zip, borra el juego de futbol e importa el menu.
public static class FutMenuAssetInstaller
{
    [MenuItem("Menu/Install FUT27 assets")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Menu] Sal de Play Mode primero."); return; }

        string zip = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "FUT27_assets.zip");
        if (!File.Exists(zip)) { Debug.LogError("[Menu] No encuentro " + zip); return; }

        // 1) Extraer a carpeta temporal y validar ANTES de borrar nada
        string tmp = Path.Combine(Path.GetTempPath(), "FUT27_assets_extract");
        if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
        ZipFile.ExtractToDirectory(zip, tmp);

        string src = Path.Combine(tmp, "FUT27_assets");
        string logo = Path.Combine(src, "images", "logo_fut27.png");
        string fondo = Path.Combine(src, "images", "fondo_estadio.jpg");
        string audioDir = Path.Combine(src, "audio");
        if (!File.Exists(logo) || !File.Exists(fondo) || !Directory.Exists(audioDir) || Directory.GetFiles(audioDir, "*.wav").Length != 5)
        {
            Debug.LogError("[Menu] El zip no trae lo esperado (logo, fondo y 5 audios). No toque nada.");
            return;
        }
        Debug.Log("[Menu] Zip extraido y validado.");

        // 2) Quitar el juego de futbol
        if (EditorSceneManager.GetActiveScene().path == "Assets/Scenes/FootballMatch.unity")
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        bool d1 = AssetDatabase.DeleteAsset("Assets/Scenes/FootballMatch.unity");
        bool d2 = AssetDatabase.DeleteAsset("Assets/Fut27");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != "Assets/Scenes/FootballMatch.unity").ToArray();
        Debug.Log("[Menu] Juego borrado: escena=" + d1 + ", carpeta Assets/Fut27=" + d2);

        // 3) Copiar los assets del menu
        string menu = Path.Combine(Application.dataPath, "Menu");
        Directory.CreateDirectory(Path.Combine(menu, "Images"));
        Directory.CreateDirectory(Path.Combine(menu, "Audio"));
        File.Copy(logo, Path.Combine(menu, "Images", "logo_fut27.png"), true);
        File.Copy(fondo, Path.Combine(menu, "Images", "fondo_estadio.jpg"), true);
        foreach (string wav in Directory.GetFiles(audioDir, "*.wav"))
            File.Copy(wav, Path.Combine(menu, "Audio", Path.GetFileName(wav)), true);
        Directory.Delete(tmp, true);
        AssetDatabase.Refresh();

        // 4) Ajustes de importacion
        var tl = AssetImporter.GetAtPath("Assets/Menu/Images/logo_fut27.png") as TextureImporter;
        if (tl != null)
        {
            tl.textureType = TextureImporterType.Sprite;
            tl.spriteImportMode = SpriteImportMode.Single;
            tl.alphaIsTransparency = true;
            tl.mipmapEnabled = false;
            tl.filterMode = FilterMode.Bilinear;
            tl.textureCompression = TextureImporterCompression.Uncompressed;
            tl.SaveAndReimport();
        }
        var tb = AssetImporter.GetAtPath("Assets/Menu/Images/fondo_estadio.jpg") as TextureImporter;
        if (tb != null)
        {
            tb.textureType = TextureImporterType.Default;
            tb.mipmapEnabled = false;
            tb.wrapMode = TextureWrapMode.Clamp;
            tb.filterMode = FilterMode.Bilinear;
            tb.textureCompression = TextureImporterCompression.CompressedHQ;
            tb.SaveAndReimport();
        }
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Menu/Audio" }))
        {
            var ai = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as AudioImporter;
            if (ai == null) continue;
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.PCM;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }

        // 5) Informe
        Debug.Log("[Menu] Logo: " + (AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/Images/logo_fut27.png") != null)
            + "  Fondo: " + (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Menu/Images/fondo_estadio.jpg") != null));
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Menu/Audio" }))
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            Debug.Log("[Menu] Audio " + c.name + "  " + c.length.ToString("0.00") + " s");
        }
        Debug.Log("[Menu] Instalacion terminada.");
    }
}

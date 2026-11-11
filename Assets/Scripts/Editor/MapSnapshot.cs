using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Gera uma imagem PNG do mapa inteiro da cena aberta (vista de cima).
// Útil para revisar a decoração e planejar regiões novas.
public static class MapSnapshot
{
    private static int PixelsPerUnit = 16;

    [MenuItem("Monterra/Gerar imagem do mapa")]
    private static void FromMenu()
    {
        string path = Render(Path.Combine(Path.GetDirectoryName(Application.dataPath), "MapSnapshot.png"));
        EditorUtility.RevealInFinder(path);
    }

    // Uso em linha de comando:
    // Unity.exe -batchmode -projectPath . -executeMethod MapSnapshot.Batch -scene Assets/Scenes/SampleScene.unity -out C:/map.png -quit
    public static void Batch()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string scene = "Assets/Scenes/SampleScene.unity";
        string output = "MapSnapshot.png";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-scene") scene = args[i + 1];
            if (args[i] == "-out") output = args[i + 1];
            if (args[i] == "-ppu") PixelsPerUnit = int.Parse(args[i + 1]);
        }

        EditorSceneManager.OpenScene(scene);
        Render(output);
        DumpTilemaps(Path.ChangeExtension(output, ".txt"));
    }

    private static string Render(string output)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (r is TilemapRenderer tr)
            {
                Tilemap tm = tr.GetComponent<Tilemap>();
                tm.CompressBounds();
                if (tm.GetUsedTilesCount() == 0) continue;
            }
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }

        int width = Mathf.Min(8192, Mathf.CeilToInt(bounds.size.x * PixelsPerUnit));
        int height = Mathf.Min(8192, Mathf.CeilToInt(bounds.size.y * PixelsPerUnit));

        GameObject camGo = new GameObject("MapSnapshotCamera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = bounds.size.y / 2f;
        cam.aspect = bounds.size.x / bounds.size.y;
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -50f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.magenta; // Áreas sem nada aparecem rosa
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 1000f;

        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        File.WriteAllBytes(output, tex.EncodeToPNG());
        Debug.Log($"MapSnapshot: {output} bounds min={bounds.min} max={bounds.max} size={width}x{height}");

        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(tex);
        return output;
    }

    private static void DumpTilemaps(string output)
    {
        StringBuilder sb = new StringBuilder();
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
            sb.AppendLine($"## {tm.name} order={(tr != null ? tr.sortingOrder : 0)} layer={(tr != null ? tr.sortingLayerName : "")} cellBounds={tm.cellBounds}");
            foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin)
            {
                TileBase tile = tm.GetTile(pos);
                if (tile == null) continue;
                Sprite sp = tm.GetSprite(pos);
                sb.AppendLine($"{pos.x},{pos.y}\t{tile.name}\t{(sp != null ? sp.name : "-")}\t{(sp != null && sp.texture != null ? sp.texture.name : "-")}");
            }
        }
        File.WriteAllText(output, sb.ToString());
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

// Refaz o NavMesh (onde os esqueletos podem andar) direto a partir dos tilemaps.
//
// Por que não usar o botão Bake do NavMeshPlus? O NavMeshPlus que está em Assets usa os mesmos
// GUIDs do pacote oficial com.unity.ai.navigation. A Unity resolve os NavMeshModifier da cena para
// o pacote, o NavMeshPlus não acha nenhum e o Bake sai vazio. Este baker não depende dos modifiers.
//
// Menu: Monterra > Refazer NavMesh
public static class MonterraNavMeshBaker
{
    // Tilemaps onde dá para andar
    private static readonly string[] Walkable = { "Grass", "Ground", "CemiterioGrama" };
    // Tilemaps que bloqueiam (árvores, muros, casas, lápides)
    private static readonly string[] Blocking = { "Props", "Cemiterio" };

    private const int NotWalkableArea = 1;

    [MenuItem("Monterra/Refazer NavMesh")]
    private static void FromMenu()
    {
        Bake();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void Bake()
    {
        NavMeshSurface2d surface = FindMainSurface();
        if (surface == null)
        {
            Debug.LogError("NavMesh: nenhum NavMeshSurface2d na cena");
            return;
        }

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        Dictionary<Sprite, Mesh> meshes = new Dictionary<Sprite, Mesh>();

        foreach (string name in Walkable)
        {
            Tilemap tm = FindTilemap(name);
            if (tm == null) continue;
            tm.CompressBounds();
            AddTiles(sources, tm, 0, meshes, false);
        }
        foreach (string name in Blocking)
        {
            Tilemap tm = FindTilemap(name);
            if (tm == null) continue;
            tm.CompressBounds();
            AddTiles(sources, tm, NotWalkableArea, meshes, true);
        }

        Bounds bounds = new Bounds(Vector3.zero, new Vector3(400f, 400f, 400f));
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(surface.GetBuildSettings(), sources, bounds,
            surface.transform.position, surface.transform.rotation);
        if (data == null)
        {
            Debug.LogError("NavMesh: a Unity não conseguiu gerar o NavMesh");
            return;
        }

        string path = surface.navMeshData != null ? AssetDatabase.GetAssetPath(surface.navMeshData) : null;
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets/Scenes/SampleScene/NavMesh-NavMesh.asset";
        }

        data.name = surface.gameObject.name;
        surface.RemoveData();
        surface.navMeshData = data;
        surface.AddData();

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        Debug.Log($"NavMesh refeito: {sources.Count} fontes, {tri.indices.Length / 3} triângulos -> {path}");
    }

    private static void AddTiles(List<NavMeshBuildSource> sources, Tilemap tm, int area, Dictionary<Sprite, Mesh> meshes, bool useSpriteShape)
    {
        Vector3 cellSize = new Vector3(tm.layoutGrid.cellSize.x, tm.layoutGrid.cellSize.y, 0f);
        foreach (Vector3Int c in tm.cellBounds.allPositionsWithin)
        {
            if (!tm.HasTile(c)) continue;

            NavMeshBuildSource src = new NavMeshBuildSource
            {
                area = area,
                transform = Matrix4x4.TRS(tm.GetCellCenterWorld(c) - tm.layoutGrid.cellGap, tm.transform.rotation, tm.transform.lossyScale)
                            * tm.orientationMatrix * tm.GetTransformMatrix(c),
            };

            Sprite sprite = useSpriteShape && tm.GetColliderType(c) == Tile.ColliderType.Sprite ? tm.GetSprite(c) : null;
            if (sprite != null)
            {
                // Obstáculo com o formato do desenho (uma flor bloqueia menos que uma árvore)
                src.shape = NavMeshBuildSourceShape.Mesh;
                src.sourceObject = GetMesh(sprite, meshes);
            }
            else
            {
                src.shape = NavMeshBuildSourceShape.Box;
                src.size = cellSize;
            }
            sources.Add(src);
        }
    }

    private static Mesh GetMesh(Sprite sprite, Dictionary<Sprite, Mesh> meshes)
    {
        if (meshes.TryGetValue(sprite, out Mesh mesh)) return mesh;

        mesh = new Mesh();
        Vector3[] vertices = new Vector3[sprite.vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = sprite.vertices[i];
        }
        int[] triangles = new int[sprite.triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = sprite.triangles[i];
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshes[sprite] = mesh;
        return mesh;
    }

    // A cena tem dois NavMeshSurface2d; o que vale é o girado em X = 270 (objeto "NavMesh")
    private static NavMeshSurface2d FindMainSurface()
    {
        NavMeshSurface2d fallback = null;
        foreach (NavMeshSurface2d s in Object.FindObjectsByType<NavMeshSurface2d>(FindObjectsInactive.Include))
        {
            if (Mathf.Approximately(s.transform.eulerAngles.x, 270f)) return s;
            fallback = s;
        }
        return fallback;
    }

    private static Tilemap FindTilemap(string name)
    {
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            if (tm.name == name) return tm;
        }
        return null;
    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Constrói o Cemiterio da Colina ao norte da Muralha Velha (ver docs/LORE.md).
// Menu: Monterra > Regioes > Construir Cemiterio da Colina.
// Pode rodar de novo: ele refaz a região do mesmo jeito (sorteios com semente fixa).
public static class CemeteryBuilder
{
    private const string TilePath = "Assets/Sprites/SUNNYSIDE_WORLD_ASSETS_V0.2/TILESET/TILEMAP/Tilemap/SunnysideWorld_Tileset_V0.1_{0}.asset";

    // Muros do cemitério (células). O interior vai de X0+1 a X1-1 e de Y0 a Y1-1.
    private const int X0 = -14, X1 = 13, Y0 = 33, Y1 = 53;
    private const int MainWallY = 32; // Muralha Velha (limite norte do mapa antigo)
    private const int RoadX0 = -2, RoadX1 = 1; // Caminho de terra que sobe da vila
    private const int RoadStartY = 23, RoadEndY = 46;

    private static readonly Color GraveyardTint = new Color(0.78f, 0.76f, 0.9f);
    private static readonly Color GraveyardGrassTint = new Color(0.6f, 0.7f, 0.62f);

    [MenuItem("Monterra/Regioes/Construir Cemiterio da Colina")]
    private static void FromMenu()
    {
        if (!EditorUtility.DisplayDialog("Monterra", "Construir (ou refazer) o Cemiterio da Colina na cena aberta?", "Construir", "Cancelar"))
        {
            return;
        }
        Build();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Linha de comando: -executeMethod CemeteryBuilder.Batch
    public static void Batch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Build();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        Tilemap grass = FindTilemap("Grass");
        Tilemap ground = FindTilemap("Ground");
        Tilemap props = FindTilemap("Props");

        Tilemap graveGrass = GetOrCreateTilemap("CemiterioGrama", grass, GraveyardGrassTint, false);
        Tilemap grave = GetOrCreateTilemap("Cemiterio", props, GraveyardTint, true);
        graveGrass.ClearAllTiles();
        grave.ClearAllTiles();

        System.Random rng = new System.Random(1907); // Semente fixa: sempre o mesmo cemitério

        // 1) Grama: estende o chão para o norte. Dentro dos muros vai para a grama "apagada" do cemitério.
        for (int x = -53; x <= 49; x++)
        {
            for (int y = Y0; y <= 62; y++)
            {
                TileBase t = T((x + y) % 2 == 0 ? 7 : 8);
                Vector3Int c = new Vector3Int(x, y, 0);
                if (x >= X0 && x <= X1 && y <= Y1)
                {
                    grass.SetTile(c, null);
                    graveGrass.SetTile(c, t);
                }
                else
                {
                    grass.SetTile(c, t);
                }
            }
        }

        // 2) Abre passagem na Muralha Velha e na fileira de árvores da borda
        for (int x = RoadX0 - 2; x <= RoadX1 + 2; x++)
        {
            for (int y = 26; y <= MainWallY; y++)
            {
                props.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
        props.SetTile(new Vector3Int(RoadX0 - 2, MainWallY, 0), T(280)); // Pilares do portão
        props.SetTile(new Vector3Int(RoadX1 + 2, MainWallY, 0), T(280));

        // 3) Caminho de terra da vila até o monumento
        int[] middle = { 71, 71, 71, 71, 70, 9, 99 };
        for (int y = RoadStartY; y <= RoadEndY; y++)
        {
            ground.SetTile(new Vector3Int(RoadX0, y, 0), T(69));
            ground.SetTile(new Vector3Int(RoadX1, y, 0), T(73));
            for (int x = RoadX0 + 1; x < RoadX1; x++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), T(middle[rng.Next(middle.Length)]));
            }
        }
        ground.SetTile(new Vector3Int(RoadX0, RoadEndY + 1, 0), null);
        ground.SetTile(new Vector3Int(RoadX0 + 1, RoadEndY + 1, 0), T(40)); // Ponta do caminho
        ground.SetTile(new Vector3Int(RoadX1 - 1, RoadEndY + 1, 0), T(42));
        ground.SetTile(new Vector3Int(RoadX1, RoadEndY + 1, 0), null);

        // 4) Muros do cemitério (mesmo desenho da Muralha Velha)
        int[] horizontal = { 262, 263, 264, 265 };
        int[] vertical = { 312, 296, 280, 335 };
        for (int x = X0; x <= X1; x++)
        {
            int id = x == X0 ? 281 : x == X1 ? 283 : horizontal[Mod(x, 4)];
            grave.SetTile(new Vector3Int(x, Y1, 0), T(id));
        }
        for (int y = Y0; y < Y1; y++)
        {
            int id = vertical[Mod(y - 26, 4)];
            grave.SetTile(new Vector3Int(X0, y, 0), T(id));
            grave.SetTile(new Vector3Int(X1, y, 0), T(id));
        }

        // 5) Árvores mortas (as normais, mas com a cor apagada do cemitério)
        Vector2Int[] deadTrees = { new Vector2Int(-12, 33), new Vector2Int(10, 33), new Vector2Int(-12, 49), new Vector2Int(10, 49), new Vector2Int(-6, 49), new Vector2Int(4, 49) };
        foreach (Vector2Int b in deadTrees)
        {
            PlaceTree(grave, b.x, b.y);
        }

        // 6) Fileiras de lápides dos dois lados do caminho
        int[] graveRows = { 37, 40, 43, 46 };
        int[] graveColumns = { -11, -9, -7, -5, 4, 6, 8, 10 };
        foreach (int y in graveRows)
        {
            foreach (int x in graveColumns)
            {
                double roll = rng.NextDouble();
                if (roll < 0.18) continue; // Algumas covas sem lápide, para não ficar perfeito demais
                if (roll < 0.35 && grave.GetTile(new Vector3Int(x + 1, y, 0)) == null && x + 1 < X1 && x != -5)
                {
                    grave.SetTile(new Vector3Int(x, y, 0), T(313)); // Lápide dupla
                    grave.SetTile(new Vector3Int(x + 1, y, 0), T(314));
                }
                else
                {
                    grave.SetTile(new Vector3Int(x, y, 0), T(roll < 0.7 ? 280 : 312));
                }
            }
        }

        // 7) Monumento no fim do caminho: o túmulo dos primeiros guardiões
        grave.SetTile(new Vector3Int(-1, 48, 0), T(353));
        grave.SetTile(new Vector3Int(0, 48, 0), T(354));
        grave.SetTile(new Vector3Int(-1, 49, 0), T(338));
        grave.SetTile(new Vector3Int(0, 49, 0), T(339));

        // 8) Pedras e tocos soltos
        int[] loose = { 232, 233, 234, 234, 246 };
        int placed = 0;
        for (int tries = 0; tries < 400 && placed < 14; tries++)
        {
            int x = rng.Next(X0 + 1, X1);
            int y = rng.Next(Y0, Y1);
            Vector3Int c = new Vector3Int(x, y, 0);
            if (x >= RoadX0 - 1 && x <= RoadX1 + 1) continue; // Caminho livre
            if (Occupied(grave, c, 1)) continue;
            grave.SetTile(c, T(loose[rng.Next(loose.Length)]));
            placed++;
        }

        // 9) Floresta fechada em volta (o jogador não sai do mapa)
        for (int x = -40; x <= 38; x += 2)
        {
            // Ao lado do cemitério as árvores encostam no muro; acima dele, só depois do muro de cima
            int firstBase = x > X0 - 2 && x <= X1 ? Y1 + 1 : 33;
            for (int baseY = firstBase; baseY <= 60; baseY += 3)
            {
                PlaceTree(props, x, baseY);
            }
        }

        // 10) Lanternas e regiões
        GameObject root = GameObject.Find("Regioes");
        if (root != null) Object.DestroyImmediate(root);
        root = new GameObject("Regioes");

        Sprite lanternSprite = AssetDatabase.LoadAssetAtPath<Tile>(string.Format(TilePath, 294)).sprite;
        TilemapRenderer propsRenderer = props.GetComponent<TilemapRenderer>();
        Vector2Int[] lanterns = { new Vector2Int(RoadX0 - 2, 38), new Vector2Int(RoadX1 + 2, 38), new Vector2Int(RoadX0 - 2, 44), new Vector2Int(RoadX1 + 2, 44) };
        for (int i = 0; i < lanterns.Length; i++)
        {
            GameObject go = new GameObject("Lanterna_" + (i + 1));
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(lanterns[i].x + 0.5f, lanterns[i].y + 0.5f, 0f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = lanternSprite;
            sr.sortingLayerID = propsRenderer.sortingLayerID;
            sr.sortingOrder = propsRenderer.sortingOrder;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.4f, 0.3f);
            col.offset = new Vector2(0f, -0.35f);
            go.AddComponent<CemeteryLantern>();
        }

        CreateRegion(root, "Vila de Monterra", false, new Vector2(-40f, -25f), new Vector2(39f, 32f));
        CreateRegion(root, "Cemiterio da Colina", true, new Vector2(X0 + 1, Y0), new Vector2(X1, Y1));

        MonterraNavMeshBaker.Bake(); // Inclui o cemitério no NavMesh
        Debug.Log("CemeteryBuilder: Cemiterio da Colina construido");
    }

    private static void PlaceTree(Tilemap tm, int x, int baseY)
    {
        // Árvore 2x3 do tileset: base (390/391), meio (381/382), copa (369/370)
        tm.SetTile(new Vector3Int(x, baseY, 0), T(390));
        tm.SetTile(new Vector3Int(x + 1, baseY, 0), T(391));
        tm.SetTile(new Vector3Int(x, baseY + 1, 0), T(381));
        tm.SetTile(new Vector3Int(x + 1, baseY + 1, 0), T(382));
        tm.SetTile(new Vector3Int(x, baseY + 2, 0), T(369));
        tm.SetTile(new Vector3Int(x + 1, baseY + 2, 0), T(370));
    }

    private static bool Occupied(Tilemap tm, Vector3Int c, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (tm.GetTile(c + new Vector3Int(dx, dy, 0)) != null) return true;
        return false;
    }

    private static void CreateRegion(GameObject root, string name, bool origin, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject("Regiao - " + name);
        go.transform.SetParent(root.transform, false);
        go.layer = 2; // Ignore Raycast: não atrapalha ferramentas nem cliques
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        go.transform.position = (min + max) / 2f;
        box.size = max - min;
        RegionArea region = go.AddComponent<RegionArea>();
        region.regionName = name;
        region.skeletonOrigin = origin;
    }

    private static Tilemap GetOrCreateTilemap(string name, Tilemap template, Color tint, bool solid)
    {
        Tilemap existing = FindTilemap(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        go.transform.SetParent(template.transform.parent, false);
        go.layer = template.gameObject.layer;
        Tilemap tm = go.AddComponent<Tilemap>();
        tm.color = tint;
        TilemapRenderer tr = go.AddComponent<TilemapRenderer>();
        EditorUtility.CopySerialized(template.GetComponent<TilemapRenderer>(), tr);

        if (solid)
        {
            TilemapCollider2D col = go.AddComponent<TilemapCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.None;
        }

        return tm;
    }

    private static Tilemap FindTilemap(string name)
    {
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            if (tm.name == name) return tm;
        }
        return null;
    }

    private static readonly Dictionary<int, TileBase> cache = new Dictionary<int, TileBase>();

    private static TileBase T(int id)
    {
        if (!cache.TryGetValue(id, out TileBase tile) || tile == null)
        {
            tile = AssetDatabase.LoadAssetAtPath<TileBase>(string.Format(TilePath, id));
            if (tile == null) Debug.LogError("Tile nao encontrado: " + id);
            cache[id] = tile;
        }
        return tile;
    }

    private static int Mod(int a, int m) => ((a % m) + m) % m;
}

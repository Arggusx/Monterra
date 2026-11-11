using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

// Faz os esqueletos aparecerem em lugares aleatórios do mapa, longe do jogador e da vila.
// De noite aparecem mais (na lore, a névoa da Ruína sobe do chão quando o sol se põe).
// Fica no objeto "Enemies" da cena. Esqueletos mortos voltam em outro lugar.
public class SkeletonSpawner : MonoBehaviour
{
    public static SkeletonSpawner Instance { get; private set; }

    [Header("Quantidade")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int maxAliveDay = 3;
    [SerializeField] private int maxAliveNight = 7;
    [SerializeField] private float spawnInterval = 6f; // Segundos entre um esqueleto e outro

    [Header("Onde podem aparecer")]
    [SerializeField] private float minDistanceFromPlayer = 12f;
    [SerializeField] private float safeRadius = 6f; // Raio livre em volta de casas, NPCs e plantações
    [SerializeField] private float borderMargin = 3f; // Distância mínima da borda do mapa
    [SerializeField] private int attemptsPerSpawn = 40;

    private readonly List<Skeleton> pool = new List<Skeleton>();
    private readonly List<Vector3> safePoints = new List<Vector3>();
    private Player player;
    private Tilemap groundTilemap; // Caminhos de terra: esqueletos não nascem neles
    private Tilemap waterTilemap;

    // Triângulos do NavMesh, para sortear pontos onde o esqueleto consegue andar
    private Vector3[] navVertices;
    private int[] navIndices;
    private float[] cumulativeArea;
    private Bounds mapBounds;

    private float timer;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();

        foreach (Tilemap tm in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
        {
            if (tm.name == "Ground") groundTilemap = tm;
            if (tm.name == "Water") waterTilemap = tm;
            if (tm.name == "Props") // A cerca de pedra da borda está nos Props
            {
                tm.CompressBounds();
                mapBounds = tm.localBounds;
                mapBounds.center = tm.transform.TransformPoint(mapBounds.center);
            }
        }

        foreach (House h in FindObjectsByType<House>(FindObjectsInactive.Include)) safePoints.Add(h.transform.position);
        foreach (NPC n in FindObjectsByType<NPC>(FindObjectsInactive.Include)) safePoints.Add(n.transform.position);
        foreach (SlotFarm s in FindObjectsByType<SlotFarm>(FindObjectsInactive.Include)) safePoints.Add(s.transform.position);

        // Esqueletos que já estejam na cena entram no controle do spawner
        foreach (Skeleton s in GetComponentsInChildren<Skeleton>(true))
        {
            pool.Add(s);
        }

        CacheNavMesh();

        // Começa com alguns já no mapa
        int start = TargetCount();
        for (int i = CountAlive(); i < start; i++)
        {
            SpawnOne();
        }
    }

    private void Update()
    {
        if (player == null || player.isDead)
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer < spawnInterval)
        {
            return;
        }
        timer = 0f;

        if (CountAlive() < TargetCount())
        {
            SpawnOne();
        }
    }

    private int TargetCount()
    {
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
        return night ? maxAliveNight : maxAliveDay;
    }

    private int CountAlive()
    {
        int count = 0;
        foreach (Skeleton s in pool)
        {
            if (s != null && s.gameObject.activeSelf && !s.isDead) count++;
        }
        return count;
    }

    private void SpawnOne()
    {
        if (!TryGetSpawnPoint(out Vector3 point))
        {
            return; // Nenhum lugar bom agora; tenta de novo no próximo intervalo
        }

        // Reaproveita um esqueleto morto/desativado antes de criar outro
        foreach (Skeleton s in pool)
        {
            if (s != null && !s.gameObject.activeSelf)
            {
                AnimationControll anim = s.GetComponentInChildren<AnimationControll>(true);
                if (anim != null)
                {
                    anim.Revive(point);
                    return;
                }
            }
        }

        if (skeletonPrefab == null)
        {
            return;
        }

        GameObject go = Instantiate(skeletonPrefab, point, Quaternion.identity, transform);
        go.name = "Skeleton (" + pool.Count + ")";
        go.SetActive(true);
        pool.Add(go.GetComponent<Skeleton>());
    }

    // Chamado pelo esqueleto quando morre: ele só some, e o spawner decide quando e onde volta
    public void OnSkeletonDied(Skeleton skeleton)
    {
        timer = 0f;
    }

    public bool TryGetSpawnPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (cumulativeArea == null || cumulativeArea.Length == 0)
        {
            CacheNavMesh();
            if (cumulativeArea == null || cumulativeArea.Length == 0) return false;
        }

        for (int i = 0; i < attemptsPerSpawn; i++)
        {
            Vector3 candidate = RandomPointOnNavMesh();
            if (IsGoodSpawn(candidate))
            {
                point = candidate;
                return true;
            }
        }
        return false;
    }

    private bool IsGoodSpawn(Vector3 p)
    {
        if (mapBounds.size != Vector3.zero)
        {
            if (p.x < mapBounds.min.x + borderMargin || p.x > mapBounds.max.x - borderMargin) return false;
            if (p.y < mapBounds.min.y + borderMargin || p.y > mapBounds.max.y - borderMargin) return false;
        }

        if (player != null && Vector2.Distance(p, player.transform.position) < minDistanceFromPlayer) return false;

        foreach (Vector3 safe in safePoints)
        {
            if (Vector2.Distance(p, safe) < safeRadius) return false;
        }

        if (groundTilemap != null && groundTilemap.HasTile(groundTilemap.WorldToCell(p))) return false;
        if (waterTilemap != null && waterTilemap.HasTile(waterTilemap.WorldToCell(p))) return false;

        // Não nasce perto de outro esqueleto (evita bando empilhado)
        foreach (Skeleton s in pool)
        {
            if (s != null && s.gameObject.activeSelf && Vector2.Distance(p, s.transform.position) < 4f) return false;
        }

        // Precisa conseguir chegar até o jogador (descarta ilhas fechadas e fora da cerca)
        if (player != null)
        {
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(p, player.transform.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }
        }

        return true;
    }

    private void CacheNavMesh()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        navVertices = tri.vertices;
        navIndices = tri.indices;

        int count = navIndices.Length / 3;
        cumulativeArea = new float[count];
        float total = 0f;
        for (int t = 0; t < count; t++)
        {
            Vector3 a = navVertices[navIndices[t * 3]];
            Vector3 b = navVertices[navIndices[t * 3 + 1]];
            Vector3 c = navVertices[navIndices[t * 3 + 2]];
            total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cumulativeArea[t] = total;
        }
    }

    // Sorteia um triângulo pelo tamanho (áreas maiores recebem mais esqueletos) e um ponto dentro dele
    private Vector3 RandomPointOnNavMesh()
    {
        float total = cumulativeArea[cumulativeArea.Length - 1];
        float r = Random.value * total;
        int t = System.Array.BinarySearch(cumulativeArea, r);
        if (t < 0) t = ~t;
        t = Mathf.Clamp(t, 0, cumulativeArea.Length - 1);

        Vector3 a = navVertices[navIndices[t * 3]];
        Vector3 b = navVertices[navIndices[t * 3 + 1]];
        Vector3 c = navVertices[navIndices[t * 3 + 2]];
        float u = Random.value, v = Random.value;
        if (u + v > 1f) { u = 1f - u; v = 1f - v; }
        Vector3 p = a + u * (b - a) + v * (c - a);
        p.z = 0f;
        return p;
    }
}

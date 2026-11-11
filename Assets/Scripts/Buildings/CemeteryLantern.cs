using System.Collections.Generic;
using UnityEngine;

// Lanterna do Cemiterio da Colina. Chegue perto e aperte E (gasta 1 madeira) para acender.
// Cada lanterna acesa diminui os esqueletos à noite. Com todas acesas, o jogador acha
// a primeira página do diário do avô (ver docs/LORE.md).
public class CemeteryLantern : MonoBehaviour
{
    public static readonly List<CemeteryLantern> All = new List<CemeteryLantern>();

    [SerializeField] private float interactRadius = 1.4f;
    [SerializeField] private int woodCost = 1;
    [SerializeField] private Color glowColor = new Color(1f, 0.72f, 0.3f, 0.55f);

    private static Sprite glowSprite;

    private SpriteRenderer body;
    private SpriteRenderer glow;
    private Player player;
    private PlayerItems items;
    private bool playerWasNear;

    public bool IsLit { get; private set; }

    public static int LitCount
    {
        get
        {
            int n = 0;
            foreach (CemeteryLantern l in All) if (l.IsLit) n++;
            return n;
        }
    }

    public static bool AllLit => All.Count > 0 && LitCount == All.Count;

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;
        body = GetComponent<SpriteRenderer>();
        glow = CreateGlow();
        Apply();
    }

    private void Update()
    {
        if (glow != null && IsLit)
        {
            // Chama tremendo; mais forte de noite
            bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;
            float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 3f, transform.position.x) * 0.3f;
            Color c = glowColor;
            c.a = glowColor.a * flicker * (night ? 1.3f : 0.6f);
            glow.color = c;
            glow.transform.localScale = Vector3.one * (0.95f + flicker * 0.1f);
        }

        if (player == null || items == null || player.isDead)
        {
            return;
        }

        bool near = Vector2.Distance(player.transform.position, transform.position) <= interactRadius;
        if (near && !playerWasNear && !IsLit)
        {
            Message($"E: acender lanterna ({woodCost} madeira)");
        }
        playerWasNear = near;

        if (near && !IsLit && !player.isPaused && GameInput.InteractPressed)
        {
            TryLight();
        }
    }

    private void TryLight()
    {
        if (items.totalWood < woodCost)
        {
            Message($"Precisa de {woodCost} madeira");
            return;
        }

        items.totalWood -= woodCost;
        SetLit(true);

        if (AllLit)
        {
            Message("Todas acesas! Voce achou uma pagina do diario do avo", 5f);
        }
        else
        {
            Message($"Lanterna acesa ({LitCount}/{All.Count})");
        }
    }

    public void SetLit(bool lit)
    {
        IsLit = lit;
        Apply();
    }

    private void Apply()
    {
        if (body != null)
        {
            body.color = IsLit ? Color.white : new Color(0.55f, 0.55f, 0.65f);
        }
        if (glow != null)
        {
            glow.enabled = IsLit;
        }
    }

    // Brilho redondo criado por código (não precisa de sprite extra no projeto)
    private SpriteRenderer CreateGlow()
    {
        if (glowSprite == null)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f); // 4 unidades de largura
        }

        GameObject go = new GameObject("Glow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = glowSprite;
        sr.color = glowColor;
        if (body != null)
        {
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 1;
        }
        return sr;
    }

    private static void Message(string text, float seconds = 2.5f)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage(text, seconds);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}

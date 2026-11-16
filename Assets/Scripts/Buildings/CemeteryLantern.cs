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
    [SerializeField] private float lightRadius = 4.5f; // Alcance da luz em unidades (tiles)
    [SerializeField] private Color haloColor = new Color(1f, 0.62f, 0.25f, 0.45f);
    [SerializeField] private Color coreColor = new Color(1f, 0.88f, 0.55f, 0.75f);

    private SpriteRenderer body;
    private LightOverlay.Light halo; // Luz larga e alaranjada
    private LightOverlay.Light core; // Centro claro em volta da chama
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
        halo = LightOverlay.Add(transform, new Vector2(0f, 0.4f), lightRadius, haloColor);
        core = LightOverlay.Add(transform, new Vector2(0f, 0.45f), lightRadius * 0.35f, coreColor);
        Apply();
    }

    private void Update()
    {
        if (IsLit && halo != null)
        {
            // Chama tremendo
            float flicker = 0.85f + Mathf.PerlinNoise(Time.time * 3f, transform.position.x) * 0.3f;
            halo.intensity = flicker;
            core.intensity = flicker;
            halo.radius = lightRadius * (0.96f + flicker * 0.06f);
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
        if (!items.Spend(ItemIds.Wood, woodCost))
        {
            Message($"Precisa de {woodCost} madeira");
            return;
        }

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
        if (halo != null)
        {
            halo.intensity = IsLit ? 1f : 0f;
            core.intensity = IsLit ? 1f : 0f;
        }
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

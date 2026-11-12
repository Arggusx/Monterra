using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ciclo de dia e noite: avança o relógio do jogo, escurece/colore a tela conforme a hora
// e mostra "Dia X - HH:MM" e as moedas no canto da tela.
// É criado automaticamente pelo GameSystems (não precisa colocar na cena).
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Tempo")]
    [Tooltip("Quantos segundos reais dura 1 hora do jogo. 30 = um dia inteiro em 12 minutos.")]
    public float realSecondsPerGameHour = 30f;
    public int day = 1;
    [Range(0f, 24f)] public float hour = 7f; // Começa às 7h da manhã

    // Disparado quando vira o dia (meia-noite). Usado pelo salvamento automático.
    public event Action<int> OnNewDay;

    // Quantas horas do jogo passaram neste frame. Usado pelas plantas para crescer com o tempo.
    public static float GameHoursThisFrame =>
        Time.deltaTime / (Instance != null ? Instance.realSecondsPerGameHour : 30f);

    // Cores da tela ao longo do dia (alfa = intensidade do escurecimento)
    private static readonly (float hour, Color color)[] skyKeys =
    {
        (0f,    new Color(0.05f, 0.07f, 0.22f, 0.55f)), // Madrugada
        (5f,    new Color(0.05f, 0.07f, 0.22f, 0.55f)),
        (6.5f,  new Color(1.00f, 0.60f, 0.35f, 0.20f)), // Amanhecer
        (8f,    new Color(1.00f, 1.00f, 1.00f, 0.00f)), // Dia claro
        (17f,   new Color(1.00f, 1.00f, 1.00f, 0.00f)),
        (18.5f, new Color(1.00f, 0.45f, 0.25f, 0.25f)), // Pôr do sol
        (20f,   new Color(0.05f, 0.07f, 0.22f, 0.50f)), // Noite
        (24f,   new Color(0.05f, 0.07f, 0.22f, 0.55f)),
    };

    private Image overlay;
    private TextMeshProUGUI clockText;
    private TextMeshProUGUI messageText;
    private float messageTimer;
    private PlayerItems playerItems;

    private void Awake()
    {
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        playerItems = FindAnyObjectByType<PlayerItems>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        hour += GameHoursThisFrame;

        if (hour >= 24f)
        {
            hour -= 24f;
            day++;
            OnNewDay?.Invoke(day);
        }

        overlay.color = SkyColor(hour);

        int h = Mathf.FloorToInt(hour);
        int m = Mathf.FloorToInt((hour - h) * 60f / 10f) * 10; // Relógio de 10 em 10 minutos
        string coins = playerItems != null ? $"\nMoedas: {playerItems.coins}\nSemente: {playerItems.SelectedSeedLabel}" : "";
        clockText.text = $"Dia {day} - {h:00}:{m:00}{coins}";

        if (messageTimer > 0f)
        {
            messageTimer -= Time.unscaledDeltaTime;
            if (messageTimer <= 0f)
            {
                messageText.text = "";
            }
        }
    }

    public bool IsNight => hour >= 20f || hour < 6f;

    // Quão escuro está agora (0 = dia claro, ~0.55 = madrugada). Usado pelas luzes.
    public float Darkness => overlay != null ? overlay.color.a : 0f;

    // Mostra um aviso rápido na tela (ex.: "Jogo salvo")
    public void ShowMessage(string text, float seconds = 2f)
    {
        messageText.text = UIFactory.Plain(text);
        messageTimer = seconds;
    }

    private static Color SkyColor(float h)
    {
        for (int i = 0; i < skyKeys.Length - 1; i++)
        {
            if (h >= skyKeys[i].hour && h <= skyKeys[i + 1].hour)
            {
                float t = Mathf.InverseLerp(skyKeys[i].hour, skyKeys[i + 1].hour, h);
                return Color.Lerp(skyKeys[i].color, skyKeys[i + 1].color, t);
            }
        }
        return skyKeys[0].color;
    }

    private void BuildUI()
    {
        // Camada de cor por baixo do HUD (ordem -10) e por cima do mundo
        Canvas tintCanvas = UIFactory.CreateCanvas("DayNight_Tint", -10, transform);
        overlay = UIFactory.CreateImage(tintCanvas.transform, "Overlay", Color.clear);
        UIFactory.Stretch(overlay.rectTransform);
        overlay.raycastTarget = false; // Não bloqueia cliques

        // Relógio por cima de tudo, no centro do topo (os cantos já são do HUD de ferramentas e itens)
        Canvas clockCanvas = UIFactory.CreateCanvas("DayNight_Clock", 50, transform);

        Image box = UIFactory.CreateImage(clockCanvas.transform, "ClockBox", new Color(0f, 0f, 0f, 0.45f));
        box.raycastTarget = false;
        RectTransform boxRect = box.rectTransform;
        boxRect.anchorMin = boxRect.anchorMax = boxRect.pivot = new Vector2(0.5f, 1f);
        boxRect.anchoredPosition = new Vector2(0f, -12f);
        boxRect.sizeDelta = new Vector2(300f, 96f);

        clockText = UIFactory.CreateText(box.transform, "Clock", 22, TextAlignmentOptions.Center);
        UIFactory.Stretch(clockText.rectTransform);

        // Mensagens rápidas logo abaixo do relógio (embaixo fica a caixa de diálogo)
        messageText = UIFactory.CreateText(clockCanvas.transform, "Message", 30, TextAlignmentOptions.Center);
        RectTransform msgRect = messageText.rectTransform;
        msgRect.anchorMin = new Vector2(0.5f, 1f);
        msgRect.anchorMax = new Vector2(0.5f, 1f);
        msgRect.pivot = new Vector2(0.5f, 1f);
        msgRect.anchoredPosition = new Vector2(0f, -116f);
        msgRect.sizeDelta = new Vector2(600f, 50f);
        messageText.text = "";
    }
}

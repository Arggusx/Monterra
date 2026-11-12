using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Loja da Jasmine: compra de sementes e venda da colheita.
// Na primeira vez que o jogador fala com ela, toca o diálogo normal (que apresenta a loja).
// Depois disso, apertar E perto dela abre a loja. Criada automaticamente pelo GameSystems.
public class ShopUI : MonoBehaviour
{
    public const string ShopkeeperName = "Jasmine";

    public static ShopUI Instance { get; private set; }

    [SerializeField] private float openRange = 1.6f;

    private GameObject panel;
    private TextMeshProUGUI infoText;
    private readonly List<TextMeshProUGUI> seedLabels = new List<TextMeshProUGUI>();
    private Button firstButton;

    private Player player;
    private PlayerItems items;
    private NPC_Dialogue shopkeeperDialogue;
    private bool pausedByShop;
    private bool wasTalking;
    private bool wasNear;
    private int closedFrame = -1;

    // Já ouviu a apresentação da Jasmine (salvo no save)
    public bool Unlocked { get; set; }

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;

        foreach (NPC_Dialogue npc in FindObjectsByType<NPC_Dialogue>(FindObjectsInactive.Include))
        {
            if (npc.dialogue != null && npc.dialogue.dialogues.Count > 0 && npc.dialogue.dialogues[0].actorName == ShopkeeperName)
            {
                shopkeeperDialogue = npc;
            }
        }

        BuildUI();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (player == null || items == null || shopkeeperDialogue == null)
        {
            return;
        }

        if (IsOpen)
        {
            Refresh();
            if (CancelPressed())
            {
                Close();
            }
            return;
        }

        bool talking = DialogueControl.instance != null && DialogueControl.instance.isShowing;
        bool near = Vector2.Distance(player.transform.position, shopkeeperDialogue.transform.position) <= openRange;

        // Terminou a primeira conversa com a Jasmine: a loja fica liberada
        if (wasTalking && !talking && near && !Unlocked)
        {
            Unlocked = true;
            Message("Loja liberada! Fale com a Jasmine para comprar sementes");
        }
        wasTalking = talking;

        // Com a loja liberada, o E abre a loja em vez de repetir o diálogo
        shopkeeperDialogue.enabled = !Unlocked;

        if (Unlocked && near && !wasNear)
        {
            Message("E: loja da Jasmine");
        }
        wasNear = near;

        if (Unlocked && near && !talking && !player.isPaused && !player.isDead
            && Time.frameCount != closedFrame && GameInput.InteractPressed)
        {
            Open();
        }
    }

    // Esc ou B (Bolinha) fecham a loja
    private static bool CancelPressed()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var pad = UnityEngine.InputSystem.Gamepad.current;
        return (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
    }

    private void Open()
    {
        panel.SetActive(true);
        player.isPaused = true;
        pausedByShop = true;
        Refresh();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private void Close()
    {
        panel.SetActive(false);
        closedFrame = Time.frameCount;
        if (pausedByShop)
        {
            player.isPaused = false;
            pausedByShop = false;
        }
    }

    private void Buy(int crop)
    {
        int price = CropDatabase.All[crop].seedPrice;
        if (items.coins < price)
        {
            Message("Moedas insuficientes");
            return;
        }
        items.coins -= price;
        items.seeds[crop]++;
        items.selectedSeed = crop; // Já deixa escolhida para plantar
    }

    private void SellHarvest()
    {
        int earned = items.SellAllHarvest();
        Message(earned > 0 ? $"Vendido! +{earned} moedas" : "Nada para vender (cenouras ficam para comer)");
    }

    private void Refresh()
    {
        int pending = 0;
        for (int i = 0; i < CropDatabase.Count; i++)
        {
            if (i != CropDatabase.Carrot) pending += items.harvest[i] * CropDatabase.All[i].sellPrice;
        }
        infoText.text = $"Moedas: {items.coins}     Colheita para vender: {pending} moedas";

        for (int i = 0; i < seedLabels.Count; i++)
        {
            CropDatabase.Crop c = CropDatabase.All[i];
            seedLabels[i].text = $"{c.name}  -  {c.seedPrice} moeda{(c.seedPrice > 1 ? "s" : "")}   (tem {items.seeds[i]}, vende por {c.sellPrice}, {c.growthHours:0.#}h)";
        }
    }

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("Shop_Canvas", 100, transform);

        Image bg = UIFactory.CreateImage(canvas.transform, "Panel", new Color(0.12f, 0.09f, 0.06f, 0.94f));
        panel = bg.gameObject;
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(820f, 560f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 18, 18);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = UIFactory.CreateText(panel.transform, "Title", 34, TextAlignmentOptions.Center);
        title.text = "Loja da Jasmine";
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        infoText = UIFactory.CreateText(panel.transform, "Info", 22, TextAlignmentOptions.Center);
        infoText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        TextMeshProUGUI hint = UIFactory.CreateText(panel.transform, "Hint", 18, TextAlignmentOptions.Center);
        hint.text = "Comprar sementes (plante com E num buraco cavado; V troca a semente)";
        hint.color = new Color(1f, 0.85f, 0.55f);
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        for (int i = 0; i < CropDatabase.Count; i++)
        {
            int crop = i; // Cópia para o botão lembrar qual planta é
            Button b = UIFactory.CreateButton(panel.transform, CropDatabase.All[i].name, () => Buy(crop));
            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>();
            label.fontSize = 19;
            seedLabels.Add(label);
            if (i == 0) firstButton = b;
        }

        UIFactory.CreateButton(panel.transform, "Vender toda a colheita", SellHarvest);
        UIFactory.CreateButton(panel.transform, "Fechar", Close);
    }

    private static void Message(string text)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage(text, 2.5f);
        }
    }
}

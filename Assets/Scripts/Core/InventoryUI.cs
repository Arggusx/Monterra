using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Inventário: abre com I / Tab (ou Select no controle).
// Mostra os itens, permite comer e vender (como a "caixa de envio" de jogos de fazenda).
// É criado automaticamente pelo GameSystems.
public class InventoryUI : MonoBehaviour
{
    // Preço de venda de cada item (em moedas)
    public const int CarrotPrice = 5;
    public const int FishPrice = 8;
    public const int WoodPrice = 2;

    private GameObject panel;
    private TextMeshProUGUI itemsText;
    private Button firstButton;

    private Player player;
    private PlayerItems items;
    private bool pausedByInventory; // Só despausa o jogador se foi o inventário que pausou

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;
        BuildUI();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (player == null || items == null)
        {
            return;
        }

        if (GameInput.InventoryPressed)
        {
            if (IsOpen)
            {
                Close();
            }
            else if (!player.isPaused && !player.isDead) // Não abre durante diálogo, construção ou pesca
            {
                Open();
            }
        }

        if (IsOpen)
        {
            RefreshText();
        }
    }

    private void Open()
    {
        panel.SetActive(true);
        player.isPaused = true;
        pausedByInventory = true;

        // Seleciona o primeiro botão para dar para navegar com o controle
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private void Close()
    {
        panel.SetActive(false);
        if (pausedByInventory)
        {
            player.isPaused = false;
            pausedByInventory = false;
        }
    }

    private void RefreshText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Madeira:  {items.totalWood} / {items.woodLimit}");
        sb.AppendLine($"Cenouras: {items.carrots} / {items.carrotLimit}");
        sb.AppendLine($"Peixes:   {items.fishes} / {items.fishesLimit}");
        sb.AppendLine($"Agua:     {Mathf.FloorToInt(items.currentWater)} / {items.waterLimit}");
        sb.AppendLine($"Moedas:   {items.coins}");
        sb.Append($"Nivel {items.currentLevel}  |  Fome {Mathf.CeilToInt(player.currentHunger)} / {player.maxHunger}");
        itemsText.text = sb.ToString();
    }

    #region Ações dos botões

    private void EatCarrot() => player.foodCarrot();

    private void EatFish() => player.foodFish();

    private void SellCarrot()
    {
        if (items.carrots > 0)
        {
            items.carrots--;
            items.coins += CarrotPrice;
        }
    }

    private void SellFish()
    {
        if (items.fishes > 0)
        {
            items.fishes--;
            items.coins += FishPrice;
        }
    }

    private void SellWood()
    {
        if (items.totalWood > 0)
        {
            items.totalWood--;
            items.coins += WoodPrice;
        }
    }

    #endregion

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("Inventory_Canvas", 100, transform);

        Image bg = UIFactory.CreateImage(canvas.transform, "Panel", new Color(0.12f, 0.09f, 0.06f, 0.92f));
        panel = bg.gameObject;
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 520f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 18, 18);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = UIFactory.CreateText(panel.transform, "Title", 34, TextAlignmentOptions.Center);
        title.text = "Inventario"; // A fonte pixelada não tem acentos
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        itemsText = UIFactory.CreateText(panel.transform, "Items", 24, TextAlignmentOptions.TopLeft);
        itemsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 170f;

        firstButton = UIFactory.CreateButton(panel.transform, "Comer cenoura (G)", EatCarrot);
        UIFactory.CreateButton(panel.transform, "Comer peixe (F)", EatFish);
        UIFactory.CreateButton(panel.transform, $"Vender cenoura (+{CarrotPrice})", SellCarrot);
        UIFactory.CreateButton(panel.transform, $"Vender peixe (+{FishPrice})", SellFish);
        UIFactory.CreateButton(panel.transform, $"Vender madeira (+{WoodPrice})", SellWood);
        UIFactory.CreateButton(panel.transform, "Fechar (I)", Close);
    }
}

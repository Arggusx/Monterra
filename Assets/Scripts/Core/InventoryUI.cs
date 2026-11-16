using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Inventário em slots: abre com I / Tab (ou Select no controle).
// - Mouse: arraste um item para outro slot (junta pilhas iguais ou troca de lugar). Passe o mouse para ver a descrição.
// - Controle: escolha um slot, use o botão "Mover" e escolha o destino.
// - Clique num item para comer, vender ou escolher a semente de plantio.
// É criado automaticamente pelo GameSystems e só desenha o que está em PlayerItems.Inventory.
public class InventoryUI : MonoBehaviour
{
    public const int Columns = 8;
    private const float Cell = 64f;
    private const float Gap = 6f;

    private static readonly Color SlotColor = new Color(0.25f, 0.18f, 0.12f, 0.95f);
    private static readonly Color HotbarColor = new Color(0.36f, 0.27f, 0.17f, 0.95f); // Primeira fileira
    private static readonly Color SelectedColor = new Color(0.62f, 0.45f, 0.22f, 1f);
    private static readonly Color MoveColor = new Color(0.30f, 0.52f, 0.30f, 1f);
    private const string Gold = "#FFD98C";

    private Canvas canvas;
    private GameObject panel;
    private readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();
    private TextMeshProUGUI statusText;

    // Painel do item selecionado
    private TextMeshProUGUI detailName, detailText;
    private Button useButton, sellOneButton, sellAllButton, moveButton;
    private TextMeshProUGUI useLabel, sellOneLabel, sellAllLabel, moveLabel;

    // Descrição flutuante e ícone que segue o mouse ao arrastar
    private RectTransform tooltip;
    private TextMeshProUGUI tooltipText;
    private Image dragGhost;

    private Player player;
    private PlayerItems items;
    private Inventory inventory;
    private bool pausedByInventory; // Só despausa o jogador se foi o inventário que pausou

    private int selected = -1; // Slot clicado
    private int hovered = -1;  // Slot sob o mouse (ou em foco no controle)
    private int moveFrom = -1; // Slot de origem no modo "Mover"
    private int dragFrom = -1; // Slot de origem ao arrastar com o mouse

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;
        if (items == null)
        {
            enabled = false;
            return;
        }

        inventory = items.Inventory;
        inventory.OnChanged += Redraw;
        BuildUI();
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Redraw;
        }
    }

    private void Update()
    {
        if (GameInput.NextSeedPressed && !player.isDead)
        {
            items.NextSeed();
            Message("Semente: " + items.SelectedSeedLabel);
        }

        if (GameInput.InventoryPressed && (ShopUI.Instance == null || !ShopUI.Instance.IsOpen))
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
            RefreshStatus();
        }
    }

    private void Open()
    {
        panel.SetActive(true);
        player.isPaused = true;
        pausedByInventory = true;
        Redraw();
        Focus(Mathf.Max(0, selected)); // Para dar para navegar com o controle
    }

    private void Close()
    {
        moveFrom = -1;
        hovered = -1;
        EndDrag();
        tooltip.gameObject.SetActive(false);
        panel.SetActive(false);

        if (pausedByInventory)
        {
            player.isPaused = false;
            pausedByInventory = false;
        }
    }

    #region Eventos dos slots (chamados pelo InventorySlotUI)

    public void Hover(int index)
    {
        hovered = index;
        RefreshTooltip();
    }

    public void Unhover(int index)
    {
        if (hovered == index)
        {
            hovered = -1;
            RefreshTooltip();
        }
    }

    private void ClickSlot(int index)
    {
        if (moveFrom >= 0) // Segundo clique do modo "Mover": este é o destino
        {
            int from = moveFrom;
            moveFrom = -1;
            selected = index;
            inventory.Move(from, index);
        }
        else
        {
            selected = index;
        }
        Redraw();
    }

    public void BeginDrag(int index, Vector2 screenPosition)
    {
        ItemData item = ItemDatabase.Get(inventory.GetSlot(index).id);
        if (item == null)
        {
            return;
        }

        dragFrom = index;
        moveFrom = -1;
        dragGhost.sprite = item.icon;
        dragGhost.color = new Color(item.iconTint.r, item.iconTint.g, item.iconTint.b, 0.85f);
        dragGhost.gameObject.SetActive(item.icon != null);
        dragGhost.transform.position = screenPosition;
        RefreshTooltip();
    }

    public void Drag(Vector2 screenPosition)
    {
        if (dragFrom >= 0)
        {
            dragGhost.transform.position = screenPosition;
        }
    }

    public void DropOn(int index)
    {
        if (dragFrom < 0)
        {
            return;
        }

        int from = dragFrom;
        selected = index;
        inventory.Move(from, index); // Soltar fora de um slot não chama isto: o item volta sozinho
        Redraw();
    }

    public void EndDrag()
    {
        dragFrom = -1;
        if (dragGhost != null)
        {
            dragGhost.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Ações dos botões

    private void UseSelected()
    {
        ItemData item = SelectedItem();
        if (item == null)
        {
            return;
        }

        if (item.category == ItemCategory.Seed)
        {
            items.selectedSeed = item.cropIndex;
            Message("Semente: " + items.SelectedSeedLabel);
            Redraw();
        }
        else if (item.IsEdible && !player.EatFromSlot(selected))
        {
            Message("Voce nao precisa comer agora");
        }
    }

    private void SellSelected(bool all)
    {
        if (SelectedItem() == null)
        {
            return;
        }

        int earned = items.SellSlot(selected, all ? inventory.GetSlot(selected).count : 1);
        if (earned > 0)
        {
            Message($"+{earned} moedas");
        }
    }

    private void SellHarvest()
    {
        int earned = items.SellAllHarvest();
        Message(earned > 0 ? $"+{earned} moedas" : "Nada para vender");
    }

    private void ToggleMove()
    {
        if (moveFrom >= 0)
        {
            moveFrom = -1;
        }
        else if (SelectedItem() != null)
        {
            moveFrom = selected;
            Focus(selected); // Volta para a grade para escolher o destino
        }
        Redraw();
    }

    #endregion

    #region Desenho

    private ItemData SelectedItem() => selected >= 0 ? ItemDatabase.Get(inventory.GetSlot(selected).id) : null;

    private void Redraw()
    {
        if (panel == null || !panel.activeSelf)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            Color color = i == moveFrom ? MoveColor : i == selected ? SelectedColor
                : i < Inventory.HotbarSize ? HotbarColor : SlotColor;
            slots[i].Show(inventory.GetSlot(i), color);
        }

        RefreshDetails();
        RefreshTooltip();
    }

    private void RefreshDetails()
    {
        ItemData item = SelectedItem();
        if (item == null)
        {
            detailName.text = moveFrom >= 0 ? "Escolha o destino" : "Clique num item";
            detailText.text = "";
            SetButton(useButton, useLabel, "Usar", false);
            SetButton(sellOneButton, sellOneLabel, "Vender 1", false);
            SetButton(sellAllButton, sellAllLabel, "Vender todos", false);
            SetButton(moveButton, moveLabel, moveFrom >= 0 ? "Cancelar mover" : "Mover", moveFrom >= 0);
            return;
        }

        int count = inventory.GetSlot(selected).count;
        detailName.text = UIFactory.Plain($"{item.displayName} x{count}");
        detailText.text = Describe(item, true);

        if (item.category == ItemCategory.Seed)
        {
            bool planting = items.selectedSeed == item.cropIndex;
            SetButton(useButton, useLabel, planting ? "Semente escolhida" : "Plantar esta", !planting);
        }
        else
        {
            SetButton(useButton, useLabel, item.IsEdible ? "Comer" : "Usar", item.IsEdible);
        }

        SetButton(sellOneButton, sellOneLabel, item.IsSellable ? $"Vender 1 (+{item.sellPrice})" : "Vender 1", item.IsSellable);
        SetButton(sellAllButton, sellAllLabel, item.IsSellable ? $"Vender todos (+{item.sellPrice * count})" : "Vender todos", item.IsSellable);
        SetButton(moveButton, moveLabel, moveFrom >= 0 ? "Cancelar mover" : "Mover", true);
    }

    private void RefreshStatus()
    {
        statusText.text =
            $"Moedas: {items.coins}     Agua: {Mathf.FloorToInt(items.currentWater)} / {items.waterLimit}\n" +
            $"Nivel {items.currentLevel}     Fome: {Mathf.CeilToInt(player.currentHunger)} / {player.maxHunger}\n" +
            $"Plantando: {items.SelectedSeedLabel} (V troca)\n" +
            $"<color={Gold}>Arraste para organizar. A primeira fileira e a barra rapida (1 a 8)</color>";
    }

    private void RefreshTooltip()
    {
        ItemData item = hovered >= 0 && dragFrom < 0 ? ItemDatabase.Get(inventory.GetSlot(hovered).id) : null;
        tooltip.gameObject.SetActive(item != null && IsOpen);
        if (item == null)
        {
            return;
        }

        tooltipText.text = $"<color={Gold}>{UIFactory.Plain(item.displayName)}</color>\n{Describe(item, true)}";

        // Ao lado do slot, na altura do topo dele
        float offset = (Cell * 0.5f + 8f) * canvas.scaleFactor;
        tooltip.position = slots[hovered].Rect.position + new Vector3(offset, offset, 0f);
    }

    // Texto do item: descrição, o que recupera ao comer e quanto vale
    private static string Describe(ItemData item, bool withDescription)
    {
        StringBuilder sb = new StringBuilder();
        if (withDescription && !string.IsNullOrEmpty(item.description))
        {
            sb.AppendLine(UIFactory.Plain(item.description));
        }
        if (item.hungerRestore > 0f) sb.AppendLine($"Fome +{item.hungerRestore:0.#}");
        if (item.healthRestore > 0f) sb.AppendLine($"Vida +{item.healthRestore:0.#}");
        if (item.staminaRestore > 0f) sb.AppendLine($"Folego +{item.staminaRestore:0.#}");
        sb.Append(item.IsSellable ? $"Vende por {item.sellPrice}" : "Nao da para vender");
        return sb.ToString();
    }

    private static void SetButton(Button button, TextMeshProUGUI label, string text, bool enabled)
    {
        label.text = UIFactory.Plain(text);
        label.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        button.interactable = enabled;
    }

    private void Focus(int slot)
    {
        if (EventSystem.current != null && slot >= 0 && slot < slots.Count)
        {
            EventSystem.current.SetSelectedGameObject(slots[slot].gameObject);
        }
    }

    private static void Message(string text)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage(text, 1.5f);
        }
    }

    #endregion

    #region Montagem da tela

    private void BuildUI()
    {
        canvas = UIFactory.CreateCanvas("Inventory_Canvas", 100, transform);

        int rows = Mathf.CeilToInt(inventory.Size / (float)Columns);
        float gridWidth = Columns * Cell + (Columns - 1) * Gap;
        float gridHeight = rows * Cell + (rows - 1) * Gap;
        const float margin = 24f, top = 70f, detailWidth = 240f, panelHeight = 540f;
        float panelWidth = margin + gridWidth + 20f + detailWidth + margin;

        Image bg = UIFactory.CreateImage(canvas.transform, "Panel", new Color(0.12f, 0.09f, 0.06f, 0.92f));
        panel = bg.gameObject;
        RectTransform rect = bg.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(panelWidth, panelHeight);

        TextMeshProUGUI title = UIFactory.CreateText(panel.transform, "Title", 34, TextAlignmentOptions.Center);
        title.text = "Inventario"; // A fonte pixelada não tem acentos
        Place(title.rectTransform, 0f, 14f, panelWidth, 44f);

        // Grade de slots
        GameObject grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        Place((RectTransform)grid.transform, margin, top, gridWidth, gridHeight);
        GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(Cell, Cell);
        gridLayout.spacing = new Vector2(Gap, Gap);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Columns;

        for (int i = 0; i < inventory.Size; i++)
        {
            slots.Add(CreateSlot(grid.transform, i));
        }

        statusText = UIFactory.CreateText(panel.transform, "Status", 21, TextAlignmentOptions.TopLeft);
        statusText.richText = true;
        Place(statusText.rectTransform, margin, top + gridHeight + 18f, gridWidth, 130f);

        Button sellHarvest = UIFactory.CreateButton(panel.transform, "Vender toda a colheita (menos comida)", SellHarvest);
        Place((RectTransform)sellHarvest.transform, margin, panelHeight - 40f - 20f, gridWidth, 40f);

        // Coluna do item selecionado
        GameObject details = new GameObject("Details", typeof(RectTransform), typeof(VerticalLayoutGroup));
        details.transform.SetParent(panel.transform, false);
        Place((RectTransform)details.transform, margin + gridWidth + 20f, top, detailWidth, panelHeight - top - 20f);
        VerticalLayoutGroup layout = details.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        detailName = UIFactory.CreateText(details.transform, "Name", 24, TextAlignmentOptions.TopLeft);
        detailName.color = new Color(1f, 0.85f, 0.55f);
        detailName.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        detailText = UIFactory.CreateText(details.transform, "Text", 18, TextAlignmentOptions.TopLeft);
        detailText.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

        useButton = DetailButton(details.transform, "Usar", UseSelected, out useLabel);
        sellOneButton = DetailButton(details.transform, "Vender 1", () => SellSelected(false), out sellOneLabel);
        sellAllButton = DetailButton(details.transform, "Vender todos", () => SellSelected(true), out sellAllLabel);
        moveButton = DetailButton(details.transform, "Mover", ToggleMove, out moveLabel);
        DetailButton(details.transform, "Fechar (I)", Close, out _);

        // Descrição flutuante (por cima de tudo, sem bloquear o mouse)
        Image tip = UIFactory.CreateImage(canvas.transform, "Tooltip", new Color(0.05f, 0.04f, 0.03f, 0.97f));
        tip.raycastTarget = false;
        tooltip = tip.rectTransform;
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.sizeDelta = new Vector2(280f, 0f);
        VerticalLayoutGroup tipLayout = tip.gameObject.AddComponent<VerticalLayoutGroup>();
        tipLayout.padding = new RectOffset(10, 10, 8, 8);
        tipLayout.childControlWidth = true;
        tipLayout.childControlHeight = true;
        tip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        tooltipText = UIFactory.CreateText(tip.transform, "Text", 18, TextAlignmentOptions.TopLeft);
        tooltipText.richText = true;
        tip.gameObject.SetActive(false);

        dragGhost = UIFactory.CreateImage(canvas.transform, "DragGhost", Color.white);
        dragGhost.raycastTarget = false; // Senão o slot de destino não recebe o "soltar"
        dragGhost.preserveAspect = true;
        dragGhost.rectTransform.sizeDelta = new Vector2(Cell - 12f, Cell - 12f);
        dragGhost.gameObject.SetActive(false);
    }

    private InventorySlotUI CreateSlot(Transform parent, int index)
    {
        Image bg = UIFactory.CreateImage(parent, "Slot_" + index, SlotColor);
        Button button = bg.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.55f);
        colors.selectedColor = new Color(1f, 0.85f, 0.55f); // Destaque ao navegar com o controle
        colors.pressedColor = new Color(0.8f, 0.65f, 0.4f);
        button.colors = colors;
        button.onClick.AddListener(() => ClickSlot(index));

        Image icon = UIFactory.CreateImage(bg.transform, "Icon", Color.white);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        UIFactory.Stretch(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(8f, 8f);
        icon.rectTransform.offsetMax = new Vector2(-8f, -8f);

        TextMeshProUGUI count = UIFactory.CreateText(bg.transform, "Count", 18, TextAlignmentOptions.BottomRight);
        UIFactory.Stretch(count.rectTransform);
        count.rectTransform.offsetMin = new Vector2(2f, 1f);
        count.rectTransform.offsetMax = new Vector2(-4f, -2f);

        InventorySlotUI slot = bg.gameObject.AddComponent<InventorySlotUI>();
        slot.Setup(this, index, bg, icon, count);
        return slot;
    }

    private static Button DetailButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI label)
    {
        Button button = UIFactory.CreateButton(parent, text, onClick);
        label = button.GetComponentInChildren<TextMeshProUGUI>();
        label.fontSize = 19;
        return button;
    }

    // Posiciona a partir do canto de cima à esquerda do painel (x para a direita, y para baixo)
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    #endregion
}

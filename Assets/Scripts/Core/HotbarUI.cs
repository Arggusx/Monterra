using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra rápida: mostra a primeira fileira do inventário na parte de baixo da tela.
// Teclas 1 a 8, scroll do mouse ou LB/RB escolhem o slot (quem lê o controle é o Player).
// Ferramenta na mão = usa com o botão esquerdo; comida = come; semente = vira a semente de plantio.
// É criada automaticamente pelo GameSystems.
public class HotbarUI : MonoBehaviour
{
    private const float Cell = 56f;
    private const float Gap = 4f;
    private const float NameSeconds = 2f; // Tempo que o nome do item fica na tela depois de trocar

    private static readonly Color SlotColor = new Color(0.12f, 0.09f, 0.06f, 0.8f);
    private static readonly Color SelectedColor = new Color(0.95f, 0.75f, 0.35f, 0.95f);

    private class Slot
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI count;
    }

    private readonly List<Slot> slots = new List<Slot>();
    private TextMeshProUGUI nameText;

    private Player player;
    private PlayerItems items;
    private int shownSlot = -1;
    private float nameTimer;

    private void Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;
        if (items == null)
        {
            enabled = false;
            return;
        }

        UseHudToolSprites();
        BuildUI();
        items.Inventory.OnChanged += Redraw;
        Redraw();
    }

    private void OnDestroy()
    {
        if (items != null)
        {
            items.Inventory.OnChanged -= Redraw;
        }
    }

    private void Update()
    {
        if (items.hotbarSlot != shownSlot)
        {
            bool first = shownSlot < 0;
            Redraw();
            if (!first)
            {
                ShowName();
            }
        }

        if (nameTimer > 0f)
        {
            nameTimer -= Time.unscaledDeltaTime;
            if (nameTimer <= 0f)
            {
                nameText.text = "";
            }
        }
    }

    // Os itens de ferramenta passam a usar a arte que já existe no HUD da cena,
    // e os ícones antigos somem (a barra rápida fica no lugar deles).
    private void UseHudToolSprites()
    {
        HUD_Controller hud = player.GetComponent<HUD_Controller>();
        if (hud == null)
        {
            hud = FindAnyObjectByType<HUD_Controller>();
        }
        if (hud == null)
        {
            return;
        }

        for (int i = 0; i < ItemIds.Tools.Length; i++)
        {
            Sprite sprite = hud.ToolSprite(i);
            ItemData tool = ItemDatabase.Get(ItemIds.Tools[i]);
            if (sprite != null && tool != null)
            {
                tool.icon = sprite;
            }
        }
        hud.HideLegacyTools();
    }

    private void Redraw()
    {
        shownSlot = items.hotbarSlot;

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            ItemStack stack = items.Inventory.GetSlot(i);
            ItemData item = stack.IsEmpty ? null : ItemDatabase.Get(stack.id);

            bool selected = i == shownSlot;
            slot.background.color = selected ? SelectedColor : SlotColor;
            slot.background.rectTransform.localScale = selected ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;

            slot.icon.enabled = item != null && item.icon != null;
            if (slot.icon.enabled)
            {
                slot.icon.sprite = item.icon;
                slot.icon.color = item.iconTint;
            }
            slot.count.text = item != null && stack.count > 1 ? stack.count.ToString() : "";
        }
    }

    private void ShowName()
    {
        ItemData item = items.SelectedItem;
        nameText.text = item != null ? UIFactory.Plain(item.displayName) : "";
        nameTimer = NameSeconds;
    }

    private void BuildUI()
    {
        Canvas canvas = UIFactory.CreateCanvas("Hotbar_Canvas", 50, transform); // Abaixo do inventário e da loja (100)

        int size = Inventory.HotbarSize;
        float width = size * Cell + (size - 1) * Gap;

        GameObject bar = new GameObject("Hotbar", typeof(RectTransform));
        bar.transform.SetParent(canvas.transform, false);
        RectTransform barRect = (RectTransform)bar.transform;
        barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 18f);
        barRect.sizeDelta = new Vector2(width, Cell);

        for (int i = 0; i < size; i++)
        {
            // Nada aqui recebe clique: senão a barra bloquearia o uso da ferramenta naquela parte da tela
            Image bg = UIFactory.CreateImage(bar.transform, "Slot_" + (i + 1), SlotColor);
            bg.raycastTarget = false;
            RectTransform rect = bg.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(i * (Cell + Gap) + Cell * 0.5f, 0f);
            rect.sizeDelta = new Vector2(Cell, Cell);

            Image icon = UIFactory.CreateImage(bg.transform, "Icon", Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            UIFactory.Stretch(icon.rectTransform);
            icon.rectTransform.offsetMin = new Vector2(7f, 7f);
            icon.rectTransform.offsetMax = new Vector2(-7f, -7f);

            TextMeshProUGUI number = UIFactory.CreateText(bg.transform, "Number", 13, TextAlignmentOptions.TopLeft);
            number.text = (i + 1).ToString();
            number.color = new Color(1f, 0.85f, 0.55f);
            UIFactory.Stretch(number.rectTransform);
            number.rectTransform.offsetMin = new Vector2(4f, 2f);
            number.rectTransform.offsetMax = new Vector2(-2f, -2f);

            TextMeshProUGUI count = UIFactory.CreateText(bg.transform, "Count", 16, TextAlignmentOptions.BottomRight);
            UIFactory.Stretch(count.rectTransform);
            count.rectTransform.offsetMin = new Vector2(2f, 1f);
            count.rectTransform.offsetMax = new Vector2(-4f, -2f);

            slots.Add(new Slot { background = bg, icon = icon, count = count });
        }

        nameText = UIFactory.CreateText(canvas.transform, "ItemName", 20, TextAlignmentOptions.Center);
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 18f + Cell + 10f);
        nameRect.sizeDelta = new Vector2(width, 26f);
    }
}

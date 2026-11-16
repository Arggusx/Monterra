using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um quadradinho do inventário. Só desenha o slot e avisa o InventoryUI do que o jogador fez
// (passou o mouse, clicou, arrastou, soltou); as regras ficam todas no InventoryUI.
public class InventorySlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private InventoryUI owner;
    private Image background;
    private Image icon;
    private TextMeshProUGUI countText;

    public int Index { get; private set; }
    public RectTransform Rect => (RectTransform)transform;

    public void Setup(InventoryUI owner, int index, Image background, Image icon, TextMeshProUGUI countText)
    {
        this.owner = owner;
        Index = index;
        this.background = background;
        this.icon = icon;
        this.countText = countText;
    }

    public void Show(ItemStack stack, Color backgroundColor)
    {
        background.color = backgroundColor;

        ItemData item = stack.IsEmpty ? null : ItemDatabase.Get(stack.id);
        icon.enabled = item != null && item.icon != null;
        if (icon.enabled)
        {
            icon.sprite = item.icon;
            icon.color = item.iconTint;
        }
        countText.text = item != null && stack.count > 1 ? stack.count.ToString() : "";
    }

    public void OnPointerEnter(PointerEventData eventData) => owner.Hover(Index);
    public void OnPointerExit(PointerEventData eventData) => owner.Unhover(Index);
    // Só quando o foco chegou pelo direcional do controle (o mouse já tem o OnPointerEnter)
    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData) owner.Hover(Index);
    }

    public void OnDeselect(BaseEventData eventData) => owner.Unhover(Index);

    public void OnBeginDrag(PointerEventData eventData) => owner.BeginDrag(Index, eventData.position);
    public void OnDrag(PointerEventData eventData) => owner.Drag(eventData.position);
    public void OnEndDrag(PointerEventData eventData) => owner.EndDrag();
    public void OnDrop(PointerEventData eventData) => owner.DropOn(Index);
}

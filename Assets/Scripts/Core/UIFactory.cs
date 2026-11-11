using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ajudantes para montar interface por código (relógio, inventário),
// sem precisar editar a cena na mão.
public static class UIFactory
{
    private static TMP_FontAsset cachedFont;

    // Usa a mesma fonte pixel art do HUD do jogo, se existir; senão a fonte padrão do TextMesh Pro
    public static TMP_FontAsset Font
    {
        get
        {
            if (cachedFont == null)
            {
                TMP_Text existing = Object.FindAnyObjectByType<TMP_Text>(FindObjectsInactive.Include);
                cachedFont = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
            }
            return cachedFont;
        }
    }

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, float size, TextAlignmentOptions align)
    {
        TMP_FontAsset font = Font; // Busca a fonte ANTES de criar o texto novo (senão ele acharia a si mesmo)

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    public static Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        Image bg = CreateImage(parent, "Button_" + label, new Color(0.25f, 0.18f, 0.12f, 0.95f));
        Button button = bg.gameObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.55f);
        colors.selectedColor = new Color(1f, 0.85f, 0.55f); // Destaque ao navegar com o controle
        colors.pressedColor = new Color(0.8f, 0.65f, 0.4f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(bg.transform, "Label", 22, TextAlignmentOptions.Center);
        text.text = label;
        Stretch(text.rectTransform);

        LayoutElement layout = bg.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 40f;

        return button;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

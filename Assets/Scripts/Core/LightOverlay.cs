using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Luzes (lanternas, tochas, fogueiras) desenhadas POR CIMA do escuro da noite.
// O escuro do DayNightCycle é uma camada de tela sobre o mundo inteiro; um brilho desenhado
// no mundo ficaria embaixo dele e quase sumiria. Aqui cada luz é uma imagem redonda numa
// camada logo acima do escuro (e abaixo do HUD), que acompanha o objeto na tela.
public class LightOverlay : MonoBehaviour
{
    public class Light
    {
        public Transform target;
        public Vector2 offset;      // Deslocamento em unidades do mundo (ex.: altura da chama)
        public float radius;        // Raio em unidades do mundo
        public Color color;
        public float intensity = 1f; // 0 = apagada; pode variar para tremer
        internal Image image;
    }

    private static LightOverlay instance;
    private static Sprite softCircle;

    private readonly List<Light> lights = new List<Light>();
    private Canvas canvas;
    private RectTransform canvasRect;

    public static Light Add(Transform target, Vector2 offset, float radius, Color color)
    {
        if (instance == null)
        {
            instance = new GameObject("LightOverlay").AddComponent<LightOverlay>();
            instance.Build();
        }

        Light light = new Light { target = target, offset = offset, radius = radius, color = color };
        Image img = UIFactory.CreateImage(instance.canvas.transform, "Luz_" + target.name, color);
        img.sprite = SoftCircle();
        img.raycastTarget = false;
        light.image = img;
        instance.lights.Add(light);
        return light;
    }

    public static void Remove(Light light)
    {
        if (instance == null || light == null) return;
        instance.lights.Remove(light);
        if (light.image != null) Destroy(light.image.gameObject);
    }

    private void Build()
    {
        // -10 é o escuro da noite; 0 é o HUD da cena
        canvas = UIFactory.CreateCanvas("Light_Canvas", -9, transform);
        canvasRect = canvas.GetComponent<RectTransform>();
        Destroy(canvas.GetComponent<GraphicRaycaster>()); // Não bloqueia cliques
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Quanto mais escuro, mais forte a luz aparece (de dia quase não se nota)
        float darkness = DayNightCycle.Instance != null ? DayNightCycle.Instance.Darkness : 0.5f;
        float strength = Mathf.Clamp01(0.25f + darkness * 1.6f);
        float pixelsPerUnit = Screen.height / (2f * cam.orthographicSize) / Mathf.Max(canvas.scaleFactor, 0.0001f);

        for (int i = lights.Count - 1; i >= 0; i--)
        {
            Light l = lights[i];
            if (l.target == null)
            {
                if (l.image != null) Destroy(l.image.gameObject);
                lights.RemoveAt(i);
                continue;
            }

            bool visible = l.intensity > 0.01f && l.target.gameObject.activeInHierarchy;
            l.image.enabled = visible;
            if (!visible) continue;

            Vector3 world = l.target.position + (Vector3)l.offset;
            Vector3 screen = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);

            RectTransform rt = l.image.rectTransform;
            rt.anchoredPosition = local;
            float size = l.radius * 2f * pixelsPerUnit;
            rt.sizeDelta = new Vector2(size, size);

            Color c = l.color;
            c.a = l.color.a * l.intensity * strength;
            l.image.color = c;
        }
    }

    // Círculo com borda bem suave, criado por código
    private static Sprite SoftCircle()
    {
        if (softCircle != null) return softCircle;

        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Vector2 center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a))); // Suaviza a borda
            }
        }
        tex.Apply();
        softCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return softCircle;
    }
}

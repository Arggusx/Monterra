using System.Collections.Generic;
using UnityEngine;

// Tipos de planta do jogo: preço da semente, preço de venda e tempo para crescer.
// A arte das plantas novas é desenhada aqui mesmo, pixel por pixel (o pacote de sprites só tem cenoura).
// Cada letra é uma cor da paleta; '.' é transparente. 16x16, a mesma escala dos tiles.
public static class CropDatabase
{
    public class Crop
    {
        public string id;        // Estável: vira o id do item colhido ("carrot") e da semente ("seed_carrot")
        public string name;      // Sem acento: a fonte pixel art não tem
        public Color32 seedTint; // Cor do saquinho de semente no inventário
        public int seedPrice;    // Quanto a semente custa na loja
        public int sellPrice;    // Quanto vale cada unidade colhida
        public float growthHours; // Horas do jogo para crescer depois de regada
        public string[] art;     // null = usa o sprite de cenoura que já existe no canteiro
        internal Sprite sprite;
    }

    public const int Carrot = 0; // A cenoura é a única colheita que também é comida

    public static readonly Crop[] All =
    {
        new Crop { id = "carrot", name = "Cenoura", seedTint = new Color32(247, 118, 34, 255), seedPrice = 2, sellPrice = 5, growthHours = 2f, art = null },
        new Crop { id = "wheat", name = "Trigo", seedTint = new Color32(254, 231, 97, 255), seedPrice = 1, sellPrice = 3, growthHours = 1.5f, art = new[]
        {
            "................",
            "....y...y...y...",
            "...yOy.yOy.yOy..",
            "...yOy.yOy.yOy..",
            "...yOy.yOy.yOy..",
            "....Oy..Oy..Oy..",
            "....l...l...l...",
            "....l...l...l...",
            "...lG..lG..lG...",
            "....G...G...G...",
            "....G..lG...G...",
            ".....G.G...G....",
            "......GGGGG.....",
            "................",
            "................",
            "................"
        }},
        new Crop { id = "cabbage", name = "Repolho", seedTint = new Color32(99, 199, 77, 255), seedPrice = 4, sellPrice = 10, growthHours = 4f, art = new[]
        {
            "................",
            "................",
            "................",
            "................",
            "......LLLL......",
            "....LLlllLLL....",
            "...LlmLLLLmlL...",
            "..LlmLLLLLLmlL..",
            "..LlmLLmmLLmlL..",
            "..lmmLmGGmLmml..",
            "...lmmGggGmml...",
            "....llmmmmll....",
            "......llll......",
            "................",
            "................",
            "................"
        }},
        new Crop { id = "strawberry", name = "Morango", seedTint = new Color32(228, 59, 68, 255), seedPrice = 6, sellPrice = 15, growthHours = 6f, art = new[]
        {
            "................",
            "................",
            "................",
            "......l..l......",
            ".....lLllLl.....",
            "....lLGlGLGl....",
            "...lGrlGGlrGl...",
            "...lrRr.lrRrl...",
            "...rwrr.rwrRl...",
            "...rrRr.rrRr....",
            "....rR..lGrRr...",
            "........rwrrR...",
            "........rrRr....",
            ".........rR.....",
            "................",
            "................"
        }},
        new Crop { id = "pumpkin", name = "Abobora", seedTint = new Color32(254, 174, 52, 255), seedPrice = 10, sellPrice = 28, growthHours = 10f, art = new[]
        {
            "................",
            "................",
            "................",
            ".......GG.......",
            "........G.......",
            "....oOOOoOOOo...",
            "...oOOyOoOyOOo..",
            "..oOOyOOoOOyOOo.",
            "..oOOOOOoOOOOOo.",
            "..oOOOOOoOOOOOo.",
            "..ooOOOOoOOOOoo.",
            "...ooOOOoOOOoo..",
            "....ooooooooo...",
            "................",
            "................",
            "................"
        }},
    };

    public static int Count => All.Length;

    // Fases antes de ficar pronta (iguais para todas as plantas)
    private static readonly string[] SproutArt =
    {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "......l..l......",
            ".....lLl.Ll.....",
            "......lGlG......",
            ".......GG.......",
            "................",
            "................",
            "................",
            "................",
            "................"
    };

    private static readonly string[] GrowingArt =
    {
            "................",
            "................",
            "................",
            "................",
            "......l...l.....",
            ".....lLl.lLl....",
            "....lLGl.lGLl...",
            ".....lGGlGGl....",
            "......lGmGl.....",
            ".......GmG......",
            "........G.......",
            "................",
            "................",
            "................",
            "................",
            "................"
    };

    private static Sprite sprout, growing;

    public static Sprite Sprout => sprout != null ? sprout : sprout = Build(SproutArt, "Broto");
    public static Sprite Growing => growing != null ? growing : growing = Build(GrowingArt, "Crescendo");

    public static Sprite ReadySprite(int crop)
    {
        Crop c = All[crop];
        if (c.art == null) return null;
        if (c.sprite == null) c.sprite = Build(c.art, c.name);
        return c.sprite;
    }

    private static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
    {
        { 'g', new Color32(38, 92, 66, 255) },   { 'G', new Color32(62, 137, 72, 255) },
        { 'm', new Color32(77, 167, 111, 255) }, { 'l', new Color32(99, 199, 77, 255) },
        { 'L', new Color32(166, 219, 110, 255) }, { 'o', new Color32(247, 118, 34, 255) },
        { 'O', new Color32(254, 174, 52, 255) }, { 'y', new Color32(254, 231, 97, 255) },
        { 'Y', new Color32(255, 246, 180, 255) }, { 'r', new Color32(228, 59, 68, 255) },
        { 'R', new Color32(162, 38, 51, 255) },  { 'w', new Color32(255, 255, 255, 255) },
        { 'b', new Color32(184, 111, 80, 255) }, { 'B', new Color32(116, 63, 57, 255) },
        { 'k', new Color32(24, 20, 37, 255) },
        { 'c', new Color32(0, 153, 219, 255) },  { 'C', new Color32(18, 78, 137, 255) },
        { 's', new Color32(139, 155, 180, 255) }, { 'S', new Color32(90, 105, 136, 255) },
    };

    // Também usado pelo ItemDatabase para desenhar os ícones dos itens
    public static Sprite Build(string[] rows, string name)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // Pixel art nítido
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            string row = rows[y];
            for (int x = 0; x < size; x++)
            {
                // A primeira linha do desenho é o topo da imagem
                pixels[(size - 1 - y) * size + x] = Palette.TryGetValue(row[x], out Color32 c) ? c : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        s.name = name;
        return s;
    }
}

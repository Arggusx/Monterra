using System.Collections.Generic;
using UnityEngine;

// Ids dos itens básicos. Use sempre estas constantes em vez de escrever o texto na mão.
public static class ItemIds
{
    public const string Wood = "wood";
    public const string Fish = "fish";

    public static string Crop(int crop) => CropDatabase.All[crop].id;       // "carrot", "wheat"...
    public static string Seed(int crop) => "seed_" + CropDatabase.All[crop].id; // "seed_carrot"...
}

// Catálogo de todos os itens do jogo, consultado por id (Dictionary: busca em O(1)).
// Os itens básicos nascem aqui por código, com ícone desenhado pixel a pixel (mesmo esquema do CropDatabase).
// Qualquer ItemData salvo em Assets/Resources/Items entra no catálogo e substitui o básico de mesmo id.
public static class ItemDatabase
{
    private static Dictionary<string, ItemData> items;
    private static List<ItemData> ordered;

    public static IReadOnlyList<ItemData> All
    {
        get
        {
            EnsureLoaded();
            return ordered;
        }
    }

    // Retorna null se o id não existe
    public static ItemData Get(string id)
    {
        EnsureLoaded();
        return !string.IsNullOrEmpty(id) && items.TryGetValue(id, out ItemData item) ? item : null;
    }

    public static bool Exists(string id) => Get(id) != null;

    public static int MaxStack(string id)
    {
        ItemData item = Get(id);
        return item != null ? Mathf.Max(1, item.maxStack) : 99;
    }

    // Os itens criados por código morrem ao sair do Play; zera para recriar na próxima vez
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        items = null;
        ordered = null;
    }

    private static void EnsureLoaded()
    {
        if (items != null) return;

        items = new Dictionary<string, ItemData>();
        ordered = new List<ItemData>();

        Register(Create(ItemIds.Wood, "Madeira", "Tronco cortado. Serve para construir e acender lanternas.",
            ItemCategory.Resource, 2, CropDatabase.Build(WoodArt, "Icone_Madeira")));

        ItemData fish = Create(ItemIds.Fish, "Peixe", "Peixe fresco do lago. Mata bem a fome.",
            ItemCategory.Food, 8, CropDatabase.Build(FishArt, "Icone_Peixe"));
        fish.hungerRestore = 25f;
        Register(fish);

        Sprite seedIcon = CropDatabase.Build(SeedArt, "Icone_Semente");
        for (int i = 0; i < CropDatabase.Count; i++)
        {
            CropDatabase.Crop crop = CropDatabase.All[i];
            bool isCarrot = i == CropDatabase.Carrot;

            Sprite icon = CropDatabase.ReadySprite(i);
            if (icon == null) icon = CropDatabase.Build(CarrotArt, "Icone_" + crop.name);

            ItemData harvest = Create(ItemIds.Crop(i), crop.name,
                isCarrot ? "Colhida na fazenda. Da para comer ou vender." : "Colhida na fazenda. Vale moedas na loja.",
                isCarrot ? ItemCategory.Food : ItemCategory.Crop, crop.sellPrice, icon);
            harvest.cropIndex = i;
            if (isCarrot) harvest.hungerRestore = 3f;
            Register(harvest);

            ItemData seed = Create(ItemIds.Seed(i), "Semente de " + crop.name,
                $"Plante num buraco cavado e regue. Cresce em {crop.growthHours:0.#}h.",
                ItemCategory.Seed, 0, seedIcon);
            seed.iconTint = crop.seedTint;
            seed.cropIndex = i;
            Register(seed);
        }

        foreach (ItemData asset in Resources.LoadAll<ItemData>("Items"))
        {
            if (string.IsNullOrEmpty(asset.id))
            {
                Debug.LogWarning($"Item '{asset.name}' sem id: ignorado.", asset);
                continue;
            }
            Register(asset);
        }
    }

    private static ItemData Create(string id, string name, string description, ItemCategory category, int sellPrice, Sprite icon)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        item.name = id;
        item.id = id;
        item.displayName = name;
        item.description = description;
        item.category = category;
        item.sellPrice = sellPrice;
        item.icon = icon;
        return item;
    }

    private static void Register(ItemData item)
    {
        if (items.TryGetValue(item.id, out ItemData old))
        {
            ordered[ordered.IndexOf(old)] = item; // Mantém a posição do item substituído
        }
        else
        {
            ordered.Add(item);
        }
        items[item.id] = item;
    }

    #region Ícones (16x16, paleta do CropDatabase)

    private static readonly string[] WoodArt =
    {
        "................",
        "................",
        "................",
        "...BBBBBBBBBB...",
        "..BbbbbbbbbbOB..",
        ".BbbBbbbbbbOyOB.",
        ".BbbbbbbBbbOyOB.",
        ".BbBbbbbbbbOyOB.",
        ".BbbbbBbbbbOyOB.",
        ".BbbbbbbbBbOyOB.",
        "..BbbbbbbbbbOB..",
        "...BBBBBBBBBB...",
        "................",
        "................",
        "................",
        "................"
    };

    private static readonly string[] FishArt =
    {
        "................",
        "................",
        "................",
        "................",
        ".....CCCCC......",
        "...CCcccccCC..C.",
        "..CcccccccccCCcC",
        ".CcwkcccccccccsC",
        ".CcccccccccccccC",
        "..CsssssssssCCcC",
        "...CCsssssCC..C.",
        ".....CCCCC......",
        "................",
        "................",
        "................",
        "................"
    };

    private static readonly string[] CarrotArt =
    {
        "................",
        "..........l.l...",
        ".........lGlG...",
        "..........GG....",
        ".........oOo....",
        "........oOOo....",
        ".......oOOOo....",
        "......oOOOo.....",
        ".....oOOOo......",
        "....oOOOo.......",
        "...oOOo.........",
        "...oOo..........",
        "...oo...........",
        "................",
        "................",
        "................"
    };

    // Em tons de branco e cinza para receber a cor de cada planta (iconTint)
    private static readonly string[] SeedArt =
    {
        "................",
        "................",
        ".....SS..SS.....",
        "......SSSS......",
        ".....SwwwwS.....",
        "....SwwwwwwS....",
        "...SwwwwwwwwS...",
        "...SwwsswwwwS...",
        "...SwwsswwwwS...",
        "...SwwwwwwwwS...",
        "...SwwwwwwwwS...",
        "....SwwwwwwS....",
        ".....SSSSSS.....",
        "................",
        "................",
        "................"
    };

    #endregion
}

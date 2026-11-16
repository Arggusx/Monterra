using UnityEngine; // Biblioteca principal da Unity

// Tudo o que o jogador carrega. Os itens (madeira, peixe, colheita, sementes) ficam no Inventory,
// em slots com pilhas. Moedas, água do balde e XP continuam aqui porque não são itens.
public class PlayerItems : MonoBehaviour
{
    [Header("Amounts")]
    public float currentWater; // Armazena a quantidade atual de água coletada
    public int coins; // Moedas ganhas vendendo itens

    public int currentLevel = 0;
    public int currentXP;

    [Header("Limits")]
    public float waterLimit = 50;
    // Só definem quando as barras do HUD ficam cheias. O limite de verdade é o espaço do inventário.
    public float woodLimit = 5;
    public float carrotLimit = 10;
    public float fishesLimit = 5f;

    public int xpLimit = 2;

    [Header("Sementes")]
    public int selectedSeed; // Índice em CropDatabase.All: semente que vai ser plantada ao apertar E num buraco
    public int startCarrotSeeds = 5; // Jogo novo começa com algumas sementes de cenoura

    [Header("Barra rápida")]
    public int hotbarSlot; // Slot da primeira fileira do inventário que está na mão (0 a 7)

    private Inventory inventory;

    // Criado na primeira vez que alguém pede, para não depender da ordem dos Awake
    public Inventory Inventory => inventory ??= new Inventory();

    // Atalhos de leitura usados pelo HUD
    public int totalWood => Count(ItemIds.Wood);
    public int carrots => Count(ItemIds.Crop(CropDatabase.Carrot));
    public int fishes => Count(ItemIds.Fish);

    private void Awake()
    {
        selectedSeed = Mathf.Clamp(selectedSeed, 0, CropDatabase.Count - 1);
        EnsureTools(); // Primeiro: num jogo novo as ferramentas ocupam os slots 1 a 5
        if (SeedCount(CropDatabase.Carrot) == 0)
        {
            AddSeed(CropDatabase.Carrot, startCarrotSeeds);
        }
    }

    #region Itens

    public int Count(string id) => Inventory.Count(id);

    // Guarda o item; retorna false (sem guardar nada) se não couber tudo
    public bool Add(string id, int amount = 1) => Inventory.TryAdd(id, amount);

    // Gasta o item; retorna false (sem gastar nada) se não tiver o bastante
    public bool Spend(string id, int amount = 1) => Inventory.TryRemove(id, amount);

    // Vende unidades do slot clicado no inventário. Retorna as moedas ganhas.
    public int SellSlot(int slot, int amount)
    {
        ItemData item = ItemDatabase.Get(Inventory.GetSlot(slot).id);
        if (item == null || !item.IsSellable) return 0;

        int earned = Inventory.RemoveAt(slot, amount) * item.sellPrice;
        coins += earned;
        return earned;
    }

    #endregion

    #region Barra rápida

    // Item que está na mão (null se o slot escolhido estiver vazio)
    public ItemData SelectedItem => ItemDatabase.Get(Inventory.GetSlot(hotbarSlot).id);

    // Ferramenta na mão no formato antigo do Player.handlingObj; -1 se não for ferramenta
    public int SelectedTool
    {
        get
        {
            ItemData item = SelectedItem;
            return item != null && item.category == ItemCategory.Tool ? item.toolIndex : -1;
        }
    }

    public void SelectHotbar(int slot)
    {
        hotbarSlot = Mathf.Clamp(slot, 0, Inventory.HotbarSize - 1);

        // Segurar uma semente já a escolhe para plantar
        ItemData item = SelectedItem;
        if (item != null && item.category == ItemCategory.Seed)
        {
            selectedSeed = item.cropIndex;
        }
    }

    // As ferramentas não se perdem: repõe as que faltarem (jogo novo e saves de antes da barra rápida)
    public void EnsureTools()
    {
        foreach (string tool in ItemIds.Tools)
        {
            if (!Inventory.Has(tool))
            {
                Inventory.Add(tool);
            }
        }
    }

    #endregion

    #region Sementes e colheita

    public int SeedCount(int crop) => Count(ItemIds.Seed(crop));

    public bool AddSeed(int crop, int amount = 1) => Add(ItemIds.Seed(crop), amount);

    public bool UseSeed(int crop) => Spend(ItemIds.Seed(crop));

    public int GetHarvest(int crop) => Count(ItemIds.Crop(crop));

    // Guarda uma unidade colhida; retorna false se o inventário estiver cheio
    public bool AddHarvest(int crop) => Add(ItemIds.Crop(crop));

    // Quanto vale a colheita que SellAllHarvest venderia
    public int HarvestValue()
    {
        int total = 0;
        for (int i = 0; i < Inventory.Size; i++)
        {
            ItemStack s = Inventory.GetSlot(i);
            ItemData item = ItemDatabase.Get(s.id);
            if (item != null && item.category == ItemCategory.Crop) total += s.count * item.sellPrice;
        }
        return total;
    }

    // Vende tudo o que foi colhido, menos o que é comida (cenoura). Retorna as moedas ganhas.
    public int SellAllHarvest()
    {
        int earned = 0;
        for (int i = 0; i < Inventory.Size; i++)
        {
            ItemStack s = Inventory.GetSlot(i);
            ItemData item = ItemDatabase.Get(s.id);
            if (item != null && item.category == ItemCategory.Crop)
            {
                earned += Inventory.RemoveAt(i, s.count) * item.sellPrice;
            }
        }
        coins += earned;
        return earned;
    }

    // Passa para a próxima semente que o jogador tem (se não tiver nenhuma, só avança)
    public void NextSeed()
    {
        for (int step = 1; step <= CropDatabase.Count; step++)
        {
            int i = (selectedSeed + step) % CropDatabase.Count;
            if (SeedCount(i) > 0 || step == CropDatabase.Count)
            {
                selectedSeed = i;
                return;
            }
        }
    }

    public string SelectedSeedLabel => $"{CropDatabase.All[selectedSeed].name} ({SeedCount(selectedSeed)})";

    #endregion

    public void WaterLimit(float water) // Método para limitar a quantidade de água ao recarregar
    {
        // Adiciona água ao total atual, sem ultrapassar o limite
        currentWater = Mathf.Clamp(currentWater + water, 0f, waterLimit);
    }

    public void XpLimit(int xpValue) // Adiciona XP e sobe de nível quando atinge o limite
    {
        currentXP += xpValue;
        Debug.Log("XP Gained: " + xpValue);
        Debug.Log("XP Total: " + currentXP + " / " + xpLimit);
        while (currentXP >= xpLimit)
        {
            currentXP -= xpLimit;
            currentLevel++;
            xpLimit += 2; // Exemplo: dificuldade progressiva
            Debug.Log("Level Up! Now at level " + currentLevel + ", next XP goal: " + xpLimit);
        }
    }
}

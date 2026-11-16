using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dados salvos em JSON. Campos novos devem ter valor padrão para saves antigos continuarem funcionando.
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;

    // 1 = itens em contadores fixos (wood, carrots, fishes, seeds[], harvest[])
    // 2 = itens em slots (inventory)
    public int version = CurrentVersion;
    public string scene;
    public string savedAt;

    // Jogador
    public float posX, posY;
    public float health, stamina, hunger;
    public int hotbarSlot; // Slot da barra rápida que estava na mão

    // Itens
    public List<ItemStack> inventory = new List<ItemStack>(); // Slots do inventário, na ordem
    public int coins;
    public float water;
    public int level, xp, xpLimit;

    // Tempo
    public int day;
    public float hour;

    // Mundo
    public List<string> builtHouses = new List<string>();
    public List<FarmSlotData> farmSlots = new List<FarmSlotData>();
    public List<string> litLanterns = new List<string>(); // Lanternas acesas do Cemiterio da Colina

    public int selectedSeed; // Índice em CropDatabase.All
    public bool shopUnlocked;

    // Só para LER saves da versão 1 (ver SaveSystem.MigrateV1). Não são mais gravados.
    public int wood, carrots, fishes;
    public int[] seeds;
    public int[] harvest;
}

[Serializable]
public class FarmSlotData
{
    public string id;
    public int digAmount;
    public bool dugHole;
    public float currentWater;
    public float growth;
    public bool plantedCarrot; // Planta pronta para colher
    public bool seeded;
    public int crop;
}

// Salvar: F5, automaticamente a cada novo dia e ao fechar o jogo.
// Carregar: F9, e automaticamente ao abrir a cena.
// Para começar do zero no Editor: menu "Monterra > Apagar save".
public class SaveSystem : MonoBehaviour
{
    public const string FileName = "monterra_save.json";
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    public static bool HasSave => File.Exists(SavePath);

    private Player player;
    private PlayerItems items;
    private bool loaded; // Evita salvar por cima antes de terminar de carregar

    private IEnumerator Start()
    {
        player = FindAnyObjectByType<Player>();
        items = player != null ? player.GetComponent<PlayerItems>() : null;

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnNewDay += _ => Save("Dia novo - jogo salvo");
        }

        // Espera um frame para todos os Start() da cena rodarem antes de aplicar o save
        yield return null;

        if (HasSave)
        {
            Load();
        }
        loaded = true;
    }

    private void Update()
    {
        if (GameInput.SavePressed)
        {
            Save("Jogo salvo");
        }

        if (GameInput.LoadPressed)
        {
            if (HasSave)
            {
                Load();
                Message("Jogo carregado");
            }
            else
            {
                Message("Nenhum jogo salvo ainda");
            }
        }
    }

    private void OnApplicationQuit()
    {
        Save(null);
    }

    public void Save(string message)
    {
        if (!loaded || player == null || items == null || player.isDead)
        {
            return; // Não salva morto nem antes de carregar
        }

        SaveData data = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            posX = player.transform.position.x,
            posY = player.transform.position.y,
            health = player.currentHealthPlayer,
            stamina = player.currentStamina,
            hunger = player.currentHunger,
            hotbarSlot = items.hotbarSlot,
            inventory = items.Inventory.ToSaveList(),
            coins = items.coins,
            water = items.currentWater,
            level = items.currentLevel,
            selectedSeed = items.selectedSeed,
            shopUnlocked = ShopUI.Instance != null && ShopUI.Instance.Unlocked,
            xp = items.currentXP,
            xpLimit = items.xpLimit,
        };

        if (DayNightCycle.Instance != null)
        {
            data.day = DayNightCycle.Instance.day;
            data.hour = DayNightCycle.Instance.hour;
        }

        foreach (House house in FindObjectsByType<House>(FindObjectsInactive.Include))
        {
            if (house.IsBuilt)
            {
                data.builtHouses.Add(ObjectId(house));
            }
        }

        foreach (SlotFarm slot in FindObjectsByType<SlotFarm>(FindObjectsInactive.Include))
        {
            data.farmSlots.Add(slot.GetState(ObjectId(slot)));
        }

        foreach (CemeteryLantern lantern in FindObjectsByType<CemeteryLantern>(FindObjectsInactive.Include))
        {
            if (lantern.IsLit)
            {
                data.litLanterns.Add(ObjectId(lantern));
            }
        }

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            if (!string.IsNullOrEmpty(message))
            {
                Message(message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao salvar o jogo: " + e.Message);
        }
    }

    public void Load()
    {
        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (Exception e)
        {
            Debug.LogError("Save corrompido, ignorando: " + e.Message);
            return;
        }

        if (data == null || player == null || items == null)
        {
            return;
        }

        // Itens e status valem em qualquer cena
        // Arquivo sem slots mas com contadores antigos também é tratado como versão 1
        bool legacy = data.version < 2
            || (data.inventory.Count == 0 && (data.wood > 0 || data.carrots > 0 || data.fishes > 0 || (data.seeds != null && data.seeds.Length > 0)));

        if (!legacy)
        {
            items.Inventory.LoadFrom(data.inventory);
        }
        else
        {
            MigrateV1(data, items);
        }
        items.selectedSeed = Mathf.Clamp(data.selectedSeed, 0, CropDatabase.Count - 1);
        items.coins = data.coins;
        items.currentWater = data.water;
        items.currentLevel = data.level;
        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.Unlocked = data.shopUnlocked;
        }
        items.currentXP = data.xp;
        if (data.xpLimit > 0)
        {
            items.xpLimit = data.xpLimit;
        }

        player.currentHealthPlayer = data.health > 0 ? data.health : player.totalHealthPlayer;
        player.currentStamina = data.stamina;
        player.currentHunger = data.hunger;
        items.EnsureTools(); // Saves de antes da barra rápida não tinham as ferramentas como item
        items.SelectHotbar(data.hotbarSlot);

        if (DayNightCycle.Instance != null && data.day > 0)
        {
            DayNightCycle.Instance.day = data.day;
            DayNightCycle.Instance.hour = data.hour;
        }

        // Posição e mundo só se o save for desta mesma cena
        if (data.scene != SceneManager.GetActiveScene().name)
        {
            return;
        }

        Vector2 pos = new Vector2(data.posX, data.posY);
        player.transform.position = new Vector3(pos.x, pos.y, player.transform.position.z);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = pos;
        }

        HashSet<string> built = new HashSet<string>(data.builtHouses);
        foreach (House house in FindObjectsByType<House>(FindObjectsInactive.Include))
        {
            if (built.Contains(ObjectId(house)))
            {
                house.ApplyBuilt();
            }
        }

        HashSet<string> lit = new HashSet<string>(data.litLanterns);
        foreach (CemeteryLantern lantern in FindObjectsByType<CemeteryLantern>(FindObjectsInactive.Include))
        {
            lantern.SetLit(lit.Contains(ObjectId(lantern)));
        }

        Dictionary<string, FarmSlotData> slots = new Dictionary<string, FarmSlotData>();
        foreach (FarmSlotData s in data.farmSlots)
        {
            slots[s.id] = s;
        }
        foreach (SlotFarm slot in FindObjectsByType<SlotFarm>(FindObjectsInactive.Include))
        {
            if (slots.TryGetValue(ObjectId(slot), out FarmSlotData s))
            {
                slot.SetState(s);
            }
        }
    }

    // Converte os contadores do save antigo em itens do inventário novo
    private static void MigrateV1(SaveData data, PlayerItems items)
    {
        Inventory inv = items.Inventory;
        bool hadSeeds = data.seeds != null && data.seeds.Length > 0;
        int startSeeds = inv.Count(ItemIds.Seed(CropDatabase.Carrot));

        inv.Clear();
        items.EnsureTools(); // Ferramentas nos primeiros slots, como num jogo novo
        inv.Add(ItemIds.Wood, data.wood);
        inv.Add(ItemIds.Fish, data.fishes);
        inv.Add(ItemIds.Crop(CropDatabase.Carrot), data.carrots);

        for (int i = 0; i < CropDatabase.Count; i++)
        {
            if (hadSeeds && i < data.seeds.Length) inv.Add(ItemIds.Seed(i), data.seeds[i]);
            // A cenoura colhida ficava em 'carrots', não em harvest[]
            if (i != CropDatabase.Carrot && data.harvest != null && i < data.harvest.Length) inv.Add(ItemIds.Crop(i), data.harvest[i]);
        }

        if (!hadSeeds) // Save de antes da loja: mantém as sementes iniciais
        {
            inv.Add(ItemIds.Seed(CropDatabase.Carrot), startSeeds);
        }

        Debug.Log("Save da versao 1 convertido para o inventario em slots.");
    }

    public static void DeleteSave()
    {
        if (HasSave)
        {
            File.Delete(SavePath);
        }
    }

    // Identificador estável do objeto: caminho na hierarquia + posição entre os irmãos
    private static string ObjectId(Component c)
    {
        Transform t = c.transform;
        string path = t.name + "#" + t.GetSiblingIndex();
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "#" + t.GetSiblingIndex() + "/" + path;
        }
        return path;
    }

    private static void Message(string text)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage(text);
        }
        else
        {
            Debug.Log(text);
        }
    }
}

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
    public int version = 1;
    public string scene;
    public string savedAt;

    // Jogador
    public float posX, posY;
    public float health, stamina, hunger;
    public int handlingObj;

    // Itens
    public int wood, carrots, fishes, coins;
    public float water;
    public int level, xp, xpLimit;

    // Tempo
    public int day;
    public float hour;

    // Mundo
    public List<string> builtHouses = new List<string>();
    public List<FarmSlotData> farmSlots = new List<FarmSlotData>();
    public List<string> litLanterns = new List<string>(); // Lanternas acesas do Cemiterio da Colina

    // Sementes e colheita (índices de CropDatabase.All)
    public int[] seeds;
    public int[] harvest;
    public int selectedSeed;
    public bool shopUnlocked;
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
            handlingObj = player.handlingObj,
            wood = items.totalWood,
            carrots = items.carrots,
            fishes = items.fishes,
            coins = items.coins,
            water = items.currentWater,
            level = items.currentLevel,
            seeds = (int[])items.seeds.Clone(),
            harvest = (int[])items.harvest.Clone(),
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
        items.totalWood = data.wood;
        items.carrots = data.carrots;
        items.fishes = data.fishes;
        items.coins = data.coins;
        items.currentWater = data.water;
        items.currentLevel = data.level;
        if (data.seeds != null && data.seeds.Length > 0) // Saves antigos não têm sementes: fica o padrão
        {
            items.seeds = (int[])data.seeds.Clone();
            items.harvest = data.harvest != null ? (int[])data.harvest.Clone() : new int[CropDatabase.Count];
            items.selectedSeed = data.selectedSeed;
        }
        items.EnsureArrays();
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
        player.handlingObj = data.handlingObj;

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

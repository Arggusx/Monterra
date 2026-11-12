using System.Collections; // Biblioteca para uso de coleções (como corrotinas)
using System.Collections.Generic; // Biblioteca para listas e dicionários genéricos
using UnityEngine; // Biblioteca principal da Unity

public class PlayerItems : MonoBehaviour
{

    [Header("Amounts")]
    // Armazenam as quantidades de cada recuso coletado
    public int totalWood; // Armazena a quantidade total de madeira coletada
    public int carrots; // Armazena a quantidade de cenouras coletada
    public float currentWater; // Armazena a quantidade atual de água coletada
    public int fishes; // Armazena a quantidade de peixes pescados
    public int coins; // Moedas ganhas vendendo itens no inventário

    public int currentLevel = 0;
    public int currentXP;

    [Header("Limits")]
    // Limitadores de recursos encapsulados
    public float waterLimit = 50;
    public float woodLimit = 5;
    public float carrotLimit = 10;
    public float fishesLimit = 5f;

    public int xpLimit = 2;

    [Header("Sementes e colheita")]
    // Índices de CropDatabase.All. A cenoura colhida continua em 'carrots' (HUD e comer).
    public int[] seeds = new int[CropDatabase.Count];
    public int[] harvest = new int[CropDatabase.Count];
    public int selectedSeed; // Semente que vai ser plantada ao apertar E num buraco
    public int harvestLimit = 20; // Limite de cada planta (menos cenoura, que usa carrotLimit)
    public int startCarrotSeeds = 5; // Jogo novo começa com algumas sementes de cenoura

    private void Awake()
    {
        EnsureArrays();
        if (seeds[CropDatabase.Carrot] == 0)
        {
            seeds[CropDatabase.Carrot] = startCarrotSeeds;
        }
    }

    // Garante o tamanho certo (saves antigos não tinham sementes; plantas novas podem ser adicionadas)
    public void EnsureArrays()
    {
        if (seeds == null || seeds.Length != CropDatabase.Count) System.Array.Resize(ref seeds, CropDatabase.Count);
        if (harvest == null || harvest.Length != CropDatabase.Count) System.Array.Resize(ref harvest, CropDatabase.Count);
        selectedSeed = Mathf.Clamp(selectedSeed, 0, CropDatabase.Count - 1);
    }

    public int GetHarvest(int crop) => crop == CropDatabase.Carrot ? carrots : harvest[crop];

    // Guarda uma unidade colhida; retorna false se a bolsa daquela planta estiver cheia
    public bool AddHarvest(int crop)
    {
        if (crop == CropDatabase.Carrot)
        {
            if (carrots >= carrotLimit) return false;
            carrots++;
            return true;
        }
        if (harvest[crop] >= harvestLimit) return false;
        harvest[crop]++;
        return true;
    }

    // Vende tudo o que foi colhido, menos as cenouras (que servem de comida). Retorna as moedas ganhas.
    public int SellAllHarvest()
    {
        int earned = 0;
        for (int i = 0; i < harvest.Length; i++)
        {
            if (i == CropDatabase.Carrot) continue;
            earned += harvest[i] * CropDatabase.All[i].sellPrice;
            harvest[i] = 0;
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
            if (seeds[i] > 0 || step == CropDatabase.Count)
            {
                selectedSeed = i;
                return;
            }
        }
    }

    public string SelectedSeedLabel => $"{CropDatabase.All[selectedSeed].name} ({seeds[selectedSeed]})";

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

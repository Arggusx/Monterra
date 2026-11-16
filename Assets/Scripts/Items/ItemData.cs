using UnityEngine;

public enum ItemCategory
{
    Resource, // Madeira, pedra, minério
    Food,     // Dá para comer (peixe, cenoura)
    Crop,     // Colheita que só serve para vender ou cozinhar
    Seed,     // Semente para plantar
    Tool,     // Ferramentas e armas (usado a partir da hotbar)
    Quest     // Itens de história
}

// Definição de um item do jogo. É só dado: quem guarda quantidades é o Inventory.
// Os itens básicos são criados por código no ItemDatabase. Para criar ou trocar um item
// pelo Editor: botão direito > Create > Monterra > Item, e salve em Assets/Resources/Items.
// Um asset com o mesmo 'id' de um item básico substitui o básico.
[CreateAssetMenu(fileName = "NovoItem", menuName = "Monterra/Item")]
public class ItemData : ScriptableObject
{
    [Tooltip("Identificador único e estável. É o que vai para o save: não mude depois de lançar.")]
    public string id;
    public string displayName; // Sem acento: a fonte pixel art não tem
    [TextArea] public string description;
    public Sprite icon;
    public Color iconTint = Color.white;
    public ItemCategory category;
    [Min(1)] public int maxStack = 99;
    [Min(0)] public int sellPrice;

    [Header("Ao comer")]
    public float hungerRestore;
    public float healthRestore;
    public float staminaRestore;

    [Header("Plantio")]
    [Tooltip("Índice em CropDatabase.All; -1 se o item não é semente nem colheita")]
    public int cropIndex = -1;

    public bool IsEdible => hungerRestore > 0f || healthRestore > 0f || staminaRestore > 0f;
    public bool IsSellable => sellPrice > 0;
}

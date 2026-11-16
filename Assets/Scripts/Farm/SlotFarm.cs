using System.Collections; // Biblioteca para corrotinas e coleções
using System.Collections.Generic; // Suporte para listas e dicionários genéricos
using UnityEngine; // Biblioteca principal da Unity

// Canteiro de plantação: cavar -> plantar semente (E) -> regar -> esperar crescer (tempo do jogo) -> colher (E)
// A semente plantada é a escolhida no inventário (troca com V). Ver CropDatabase.
public class SlotFarm : MonoBehaviour, IInteractable
{
    // Todos os atributos que tiver '[SerializeField]' ou 'public' é definido no inspector da Unity
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip holeSFX;
    [SerializeField] private AudioClip carrotSFX;


    [Header("Components")]
    [SerializeField] private Sprite hole; // Define sprite do buraco cavado
    [SerializeField] private Sprite carrot; // Define sprite da cenoura crescida
    [SerializeField] private SpriteRenderer spriteRenderer; // Componente responsável por trocar os sprites

    [Header("Settings")]
    [SerializeField] private int digAmount; // Quantidade de vezes que precisa cavar
    [SerializeField] private float waterAmount; // Quantidade de água necessária para crescer
    [SerializeField] private bool detecting; // Se está sendo regado (detecção de água)
    [Tooltip("Água acumulada por segundo enquanto é regado (antes era 0.1 por frame = 6/s a 60 FPS)")]
    [SerializeField] private float waterPerSecond = 6f;

    private int inicialDigAmount; // Armazena a quantidade inicial para referência
    private float currentWater; // Quantidade atual de água acumulada
    private float growth; // Horas de crescimento já passadas

    PlayerItems playerItems; // Referência ao script de itens do jogador para armazenar quantidades em variáveis
    Player player;

    private bool dugHole; // Se o buraco já foi cavado
    private bool seeded; // Tem semente plantada
    private int crop; // Qual planta (índice em CropDatabase.All)
    private bool plantedCarrot; // Planta pronta para colher (o nome ficou do tempo em que só havia cenoura)
    private bool isPlayer; // É verdadeiro quando o player entra no colisor pré-setado

    private SpriteRenderer plantRenderer; // Desenho da planta por cima do buraco

    private static readonly Color growingTint = new Color(0.8f, 1f, 0.8f); // Leve verde enquanto cresce

    private bool IsWatered => currentWater >= waterAmount;
    private float GrowthHours => CropDatabase.All[crop].growthHours;

    private void Awake()
    {
        inicialDigAmount = digAmount; // Salva o valor inicial da terra (no Awake para o save poder sobrescrever)

        GameObject go = new GameObject("Planta");
        go.transform.SetParent(spriteRenderer.transform, false);
        plantRenderer = go.AddComponent<SpriteRenderer>();
        plantRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        plantRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
    }

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    private void Start()
    {
        playerItems = FindAnyObjectByType<PlayerItems>(); // Encontra o script PlayerItems na cena
        player = FindAnyObjectByType<Player>();
    }

    void Update()
    {
        if (!dugHole) // Ainda não foi cavado
        {
            return;
        }

        if (detecting && !IsWatered) // Se estiver sendo regado
        {
            currentWater += waterPerSecond * Time.deltaTime; // Acumula água por segundo (independe do FPS)
            if (IsWatered) UpdateVisual();
        }

        if (seeded && IsWatered && !plantedCarrot) // Regado: cresce com o passar das horas do jogo
        {
            growth += DayNightCycle.GameHoursThisFrame;

            if (growth >= GrowthHours)
            {
                plantedCarrot = true;
                audioSource.PlayOneShot(holeSFX);
            }
            UpdateVisual();
        }
    }

    #region Interação (E)

    // Vários canteiros podem estar encostados no jogador: o PlayerInteractor escolhe o que está à frente dele
    public bool CanInteract(Player p) => isPlayer && dugHole && (plantedCarrot || !seeded);

    public string GetPrompt(Player p) =>
        plantedCarrot ? "Colher " + CropDatabase.All[crop].name : "Plantar " + playerItems.SelectedSeedLabel;

    public Vector2 InteractPoint(Player p) => transform.position;

    public void Interact(Player p)
    {
        if (plantedCarrot)
        {
            Harvest();
        }
        else if (!seeded)
        {
            Plant();
        }
    }

    #endregion

    private void Plant()
    {
        int seed = playerItems.selectedSeed;
        string name = CropDatabase.All[seed].name;
        if (!playerItems.UseSeed(seed))
        {
            Message($"Sem sementes de {name} (V troca)");
            return;
        }

        seeded = true;
        crop = seed;
        growth = 0f;
        audioSource.PlayOneShot(holeSFX);
        UpdateVisual();
        Message(IsWatered ? $"{name} plantado" : $"{name} plantado - agora regue");
    }

    private void Harvest()
    {
        if (!playerItems.AddHarvest(crop))
        {
            Message("Inventario cheio - venda ou coma algo");
            return;
        }

        audioSource.PlayOneShot(carrotSFX);
        plantedCarrot = false; // Colheu: o buraco fica pronto para outra semente
        seeded = false;
        currentWater = 0f; // Precisa regar de novo
        growth = 0f;
        UpdateVisual();
    }

    // Mostra buraco, broto, planta crescendo ou planta pronta
    private void UpdateVisual()
    {
        if (!dugHole)
        {
            plantRenderer.sprite = null;
            return;
        }

        bool carrotReady = plantedCarrot && crop == CropDatabase.Carrot;

        spriteRenderer.sprite = carrotReady ? carrot : hole;
        spriteRenderer.color = IsWatered && !plantedCarrot ? growingTint : Color.white; // Terra molhada fica esverdeada

        if (!seeded || carrotReady)
        {
            plantRenderer.sprite = null;
        }
        else if (plantedCarrot)
        {
            plantRenderer.sprite = CropDatabase.ReadySprite(crop);
        }
        else
        {
            plantRenderer.sprite = growth >= GrowthHours * 0.5f ? CropDatabase.Growing : CropDatabase.Sprout;
        }
    }

    private static void Message(string text)
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage(text, 2.5f);
        }
    }

    public void OnHit() // Função chamada quando o jogador cava
    {
        digAmount--; // Diminui a resistência da terra

        if (digAmount <= inicialDigAmount / 2) // Quando cavou metade ou mais
        {
            bool firstTime = !dugHole;
            dugHole = true; // Marca que já pode plantar
            if (firstTime)
            {
                UpdateVisual(); // Mostra o sprite de buraco
                Message("Buraco pronto para plantar");
            }
        }
    }

    #region Salvar / Carregar

    public FarmSlotData GetState(string id)
    {
        return new FarmSlotData
        {
            id = id,
            digAmount = digAmount,
            dugHole = dugHole,
            currentWater = currentWater,
            growth = growth,
            plantedCarrot = plantedCarrot,
            seeded = seeded,
            crop = crop,
        };
    }

    public void SetState(FarmSlotData data)
    {
        digAmount = data.digAmount;
        dugHole = data.dugHole;
        currentWater = data.currentWater;
        growth = data.growth;
        plantedCarrot = data.plantedCarrot;
        seeded = data.seeded || data.plantedCarrot; // Save antigo: cenoura pronta não tinha "semente"
        crop = Mathf.Clamp(data.crop, 0, CropDatabase.Count - 1);
        UpdateVisual();
    }

    #endregion

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Dig")) // Se colidir com algo que tenha a tag "Dig"
        {
            OnHit(); // Cava
        }

        if (collision.CompareTag("Water")) // Se colidir com algo com tag "Water"
        {
            detecting = true; // Começa a regar
        }
        if (collision.CompareTag("Player"))
        {
            isPlayer = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Water")) // Se sair da área de regar
        {
            detecting = false; // Para de regar
        }
        if (collision.CompareTag("Player"))
        {
            isPlayer = false;
        }
    }
}

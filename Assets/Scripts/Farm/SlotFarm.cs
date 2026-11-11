using System.Collections; // Biblioteca para corrotinas e coleções
using System.Collections.Generic; // Suporte para listas e dicionários genéricos
using UnityEngine; // Biblioteca principal da Unity

// Canteiro de plantação: cavar -> regar -> esperar crescer (tempo do jogo) -> colher com E
public class SlotFarm : MonoBehaviour
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
    [Tooltip("Horas do jogo que a cenoura leva para crescer depois de regada")]
    [SerializeField] private float growthHours = 2f;

    private int inicialDigAmount; // Armazena a quantidade inicial para referência
    private float currentWater; // Quantidade atual de água acumulada
    private float growth; // Horas de crescimento já passadas

    PlayerItems playerItems; // Referência ao script de itens do jogador para armazenar quantidades em variáveis

    private bool dugHole; // Se o buraco já foi cavado
    private bool plantedCarrot; // Cenoura pronta para colher
    private bool isPlayer; // É verdadeiro quando o player entra no colisor pré-setado

    private static readonly Color growingTint = new Color(0.8f, 1f, 0.8f); // Leve verde enquanto cresce

    private bool IsWatered => currentWater >= waterAmount;

    private void Awake()
    {
        inicialDigAmount = digAmount; // Salva o valor inicial da terra (no Awake para o save poder sobrescrever)
    }

    private void Start()
    {
        playerItems = FindAnyObjectByType<PlayerItems>(); // Encontra o script PlayerItems na cena
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
        }

        if (IsWatered && !plantedCarrot) // Regado: cresce com o passar das horas do jogo
        {
            growth += DayNightCycle.GameHoursThisFrame;
            spriteRenderer.color = growingTint;

            if (growth >= growthHours)
            {
                spriteRenderer.sprite = carrot; // Cenoura cresceu (muda sprite)
                spriteRenderer.color = Color.white;
                audioSource.PlayOneShot(holeSFX);
                plantedCarrot = true;
            }
        }

        if (GameInput.InteractPressed && plantedCarrot && isPlayer) // Se o jogador apertar E
        {
            plantedCarrot = false; // Colheu: precisa regar de novo para crescer outra cenoura
            audioSource.PlayOneShot(carrotSFX);
            spriteRenderer.sprite = hole; // Volta para o sprite de buraco
            if (playerItems.carrots < playerItems.carrotLimit)
            {
                playerItems.carrots++; // Dá uma cenoura ao jogador (respeitando o limite)
            }
            currentWater = 0f; // Reseta a água para replantar
            growth = 0f;
        }
    }

    public void OnHit() // Função chamada quando o jogador cava
    {
        digAmount--; // Diminui a resistência da terra

        if (digAmount <= inicialDigAmount / 2) // Quando cavou metade ou mais
        {
            if (!dugHole)
            {
                spriteRenderer.sprite = hole; // Mostra o sprite de buraco
            }
            dugHole = true; // Marca que já pode plantar
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
        };
    }

    public void SetState(FarmSlotData data)
    {
        digAmount = data.digAmount;
        dugHole = data.dugHole;
        currentWater = data.currentWater;
        growth = data.growth;
        plantedCarrot = data.plantedCarrot;

        if (plantedCarrot)
        {
            spriteRenderer.sprite = carrot;
            spriteRenderer.color = Color.white;
        }
        else if (dugHole)
        {
            spriteRenderer.sprite = hole;
            spriteRenderer.color = IsWatered ? growingTint : Color.white;
        }
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

using System.Collections; // Biblioteca para corrotinas e coleções
using System.Collections.Generic; // Suporte para listas e dicionários genéricos
using UnityEngine; // Biblioteca principal da Unity

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
    [SerializeField] private float waterPerSecond = 6f; // Água recebida por segundo enquanto é regado
    [SerializeField] private bool detecting; // Se está sendo regado (detecção de água)

    private int inicialDigAmount; // Armazena a quantidade inicial para referência
    private float currentWater; // Quantidade atual de água acumulada

    PlayerItems playerItems; // Referência ao script de itens do jogador para armazenar quantidades em variáveis

    private bool dugHole; // Se o buraco já foi cavado
    private bool plantedCarrot;
    private bool isPlayer; // É verdadeiro quando o player entra no colisor pré-setado

    private void Start()
    {
        playerItems = FindObjectOfType<PlayerItems>(); // Encontra o script PlayerItems na cena
        inicialDigAmount = digAmount; // Salva o valor inicial da terra para comparação depois
    }

    void Update()
    {
        if (dugHole) // Se o buraco foi cavado
        {
            if (detecting) // Se estiver sendo regado
            {
                currentWater += waterPerSecond * Time.deltaTime; // Acumula água aos poucos (independente do FPS)
            }

            if (currentWater >= waterAmount && !plantedCarrot) // Se acumulou água suficiente
            {
                spriteRenderer.sprite = carrot; // Planta cenoura (muda sprite)
                audioSource.PlayOneShot(holeSFX);

                plantedCarrot = true;
            }
            // Se o jogador apertar E e ainda couber cenoura no inventário
            if (Input.GetKeyDown(KeyCode.E) && plantedCarrot && isPlayer && playerItems.carrots < playerItems.carrotLimit)
            {
                plantedCarrot = false; // Colheu: precisa regar de novo para crescer outra
                audioSource.PlayOneShot(carrotSFX);
                spriteRenderer.sprite = hole; // Volta para o sprite de buraco
                playerItems.carrots++; // Dá uma cenoura ao jogador
                currentWater = 0f; // Reseta a água para replantar
            }
        }
    }

    public void OnHit() // Função chamada quando o jogador cava
    {
        if (dugHole) return; // Buraco já cavado: cavar de novo apagaria a cenoura plantada

        digAmount--; // Diminui a resistência da terra

        if (digAmount <= inicialDigAmount / 2) // Quando cavou metade ou mais
        {
            spriteRenderer.sprite = hole; // Mostra o sprite de buraco
            dugHole = true; // Marca que já pode plantar
        }
    }

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

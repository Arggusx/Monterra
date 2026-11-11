using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Amounts")]
    [SerializeField] private int woodAmount;
    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;
    [SerializeField] private float timeAmount;

    [Header("Components")]
    [SerializeField] private GameObject houseCollider;
    [SerializeField] private SpriteRenderer houseSprite;
    [SerializeField] private Transform point;

    [Header("Referências")]
    private Player player;
    private PlayerAnim playerAnim;
    private PlayerItems playerItems;

    private bool detectingPlayer;
    private float timeCount;
    private bool isBegining;
    private bool isBuilt; // Evita construir a mesma casa várias vezes

    void Start()
    {
        player = FindAnyObjectByType<Player>();
        playerAnim = player.GetComponent<PlayerAnim>();
        playerItems = player.GetComponent<PlayerItems>();
    }

    void Update()
    {
        // Inicializa a construção se: detectar player na área de colisão e apertar tecla 'E' e a 
        // quantidade de madeira que coletei for maior ou igual a quantidade setada
        if (detectingPlayer && !isBegining && !isBuilt && Input.GetKeyDown(KeyCode.E) && playerItems.totalWood >= woodAmount)
        {
            isBegining = true;
            timeCount = 0f;
            playerAnim.OnHammeringStarted(); // Começa a animação de martelar
            houseSprite.color = startColor;
            player.transform.position = point.position; // Move o player para um objeto vazio transparente
            Debug.Log("Construindo!!");
            player.isPaused = true; // /Não pode se mover enquanto estiver construindo
            playerItems.totalWood -= woodAmount;
        }
        if (isBegining)
        {
            timeCount += Time.deltaTime; // Conta o tempo em segundos desde o ultimo frame 

            if (timeCount >= timeAmount)
            {
                playerAnim.OnHammeringEnded(); // Animação começa
                houseSprite.color = endColor; // Seta a cor final (sem opacidade)
                player.isPaused = false; // Player pode se mover
                houseCollider.SetActive(true); // Ativar o colisor ao terminar de construir
                isBegining = false; // Termina a construção (senão repetiria isso a cada frame)
                isBuilt = true;
            }
        }
    }
    void OnTriggerEnter2D(Collider2D collision) // Quando algo entra no gatilho
    {
        if (collision.CompareTag("Player")) // Se for o jogador
        {
            detectingPlayer = true; // Marca que o jogador está na área
        }
    }

    void OnTriggerExit2D(Collider2D collision) // Quando algo sai do gatilho
    {
        if (collision.CompareTag("Player")) // Se for o jogador
        {
            detectingPlayer = false; // Marca que o jogador saiu da área
        }
    }
}

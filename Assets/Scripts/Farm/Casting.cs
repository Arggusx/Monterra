using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Casting : MonoBehaviour
{
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private int percentage; // Chance do jogador cpnseguir pegar um peixe

    public bool detectingPlayer; // Verifica se o jogador está na área de detecção

    private PlayerItems playerItems; // Referência ao script PlayerItems
    private PlayerAnim playerAnim;
    private Player player;


    // Start é chamado antes do primeiro frame
    void Start()
    {
        playerItems = FindObjectOfType<PlayerItems>(); // Encontra o objeto com o script PlayerItems na cena
        playerAnim = playerItems.GetComponent<PlayerAnim>();
        player = playerItems.GetComponent<Player>();
    }

    // Update é chamado a cada frame
    void Update()
    {
        // Se o jogador estiver na área, com a vara de pesca (handlingObj 4) e clicar com o botão esquerdo
        if (detectingPlayer && !player.isPaused && player.handlingObj == 4 && Input.GetMouseButtonDown(0))
        {
            playerAnim.OnCastingStarded();
        }
    }

    public void OnCasting()
    {
        int randonValue = Random.Range(1, 100);
        if (randonValue <= percentage)
        {

            float direction = Random.value < 0.5f ? -1f : 1f; // Decide aleatoriamente entre esquerda (X-) ou direita (X+)

            float offsetX = Random.Range(0.3f, 2f) * direction; // Gera uma posição aleatória em um dos lados. Obs.: Player(posição 0)

            // Posição do player + 'alguma posição à esquerda(X negativo)'
            Instantiate(fishPrefab, player.transform.position + new Vector3(offsetX, 0f, 0f), Quaternion.identity);
            Debug.Log("Um peixe!!!");
        }
        else
        {
            Debug.Log("Pesca falhou");
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

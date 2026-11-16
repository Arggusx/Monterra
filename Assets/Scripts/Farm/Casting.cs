using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Área de pesca na beira do lago. Com o balde na mão, o E enche o balde aqui também.
public class Casting : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private int percentage; // Chance do jogador cpnseguir pegar um peixe

    public bool detectingPlayer; // Verifica se o jogador está na área de detecção

    private PlayerItems playerItems; // Referência ao script PlayerItems
    private PlayerAnim playerAnim;
    private Player player;
    private float fillValue = 25f; // Água por balde quando o jogador enche aqui (copiado de uma área de água)

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    // Start é chamado antes do primeiro frame
    void Start()
    {
        playerItems = FindAnyObjectByType<PlayerItems>(); // Encontra o objeto com o script PlayerItems na cena
        playerAnim = playerItems.GetComponent<PlayerAnim>();
        player = playerItems.GetComponent<Player>();

        Water water = FindAnyObjectByType<Water>();
        if (water != null)
        {
            fillValue = water.FillValue;
        }
    }

    #region Interação (E)

    public bool CanInteract(Player p) => detectingPlayer;

    public string GetPrompt(Player p) => LakeInteraction.Prompt(p, LakeInteraction.Action.Fish);

    public Vector2 InteractPoint(Player p) => p.transform.position;

    public void Interact(Player p) => LakeInteraction.Do(p, LakeInteraction.Action.Fish, fillValue);

    #endregion

    public void OnCasting()
    {
        int randonValue = Random.Range(1, 101); // 1 a 100 (o máximo é exclusivo)
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Water : MonoBehaviour
{
    [SerializeField] private bool detectingPlayer; // Verifica se o jogador está na área de detecção
    [SerializeField] private int waterValue; // Quantidade de água que será adicionada

    private PlayerItems player; // Referência ao script PlayerItems

    // Start é chamado antes do primeiro frame
    void Start()
    {
        player = FindAnyObjectByType<PlayerItems>(); // Encontra o objeto com o script em PlayerItems na cena
    }

    // Update é chamado a cada frame
    void Update()
    {
        if (detectingPlayer && Input.GetKeyDown(KeyCode.E)) // Se o jogador estiver na área e apertar E
        {
            player.WaterLimit(waterValue); // Adiciona água ao inventário do jogador
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // Se colidiu com o jogador
        {
            PlayerItems items = collision.GetComponent<PlayerItems>();
            if (items.fishes >= items.fishesLimit) return; // Inventário cheio: o peixe fica no chão

            // Aumenta a quantidade de peixes do jogador
            items.fishes++;

            // Destroi esse objeto de peixe
            Destroy(gameObject);
        }
    }
}

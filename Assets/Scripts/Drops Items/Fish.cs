using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // Se colidiu com o jogador
        {
            // Aumenta a quantidade de peixes do jogador
            PlayerItems items = collision.GetComponent<PlayerItems>();
            if (items == null)
            {
                return;
            }

            // Guarda no inventário; se não couber, o peixe fica no chão
            if (items.Add(ItemIds.Fish))
            {
                Destroy(gameObject);
            }
            else if (DayNightCycle.Instance != null)
            {
                DayNightCycle.Instance.ShowMessage("Inventario cheio", 1.5f);
            }
        }
    }
}

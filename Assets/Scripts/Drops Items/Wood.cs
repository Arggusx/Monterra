using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wood : MonoBehaviour
{
    [SerializeField] private float speed;     // Velocidade do movimento da madeira
    [SerializeField] private float timeMove;  // Tempo máximo que ela se moverá

    private float timeCount;  // Tempo que já passou desde que o objeto começou a se mover

    // Update é chamado uma vez por frame
    void Update()
    {
        timeCount += Time.deltaTime; // Soma o tempo que passou desde o último frame

        if (timeCount < timeMove) // Se ainda não atingiu o tempo máximo de movimento
        {
            // Move a madeira para a direita de forma suave
            transform.Translate(Vector2.right * speed * Time.deltaTime);
        }
    }

    // Detecta colisão com o jogador
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) // Se colidiu com o jogador
        {
            // Aumenta a quantidade de madeira do jogador
            PlayerItems items = collision.GetComponent<PlayerItems>();
            if (items != null)
            {
                items.totalWood++;
            }

            // Destroi esse objeto de madeira
            Destroy(gameObject);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : MonoBehaviour
{
    public float speed; // Velocidade de movimento do NPC
    private float inicialSpeed; // Velocidade original (usada para restaurar)
    private int index; // Índice do ponto atual da lista de caminhos
    private Animator anim; // Referência ao componente Animator

    public List<Transform> paths = new List<Transform>(); // Lista de pontos (caminhos) que o NPC deve seguir

    private void Start()
    {
        inicialSpeed = speed; // Guarda a velocidade inicial
        anim = GetComponent<Animator>(); // Pega o componente Animator no mesmo GameObject
    }

    void Update()
    {
        // Se o diálogo estiver ativo, para o NPC
        if (paths.Count == 0)
        {
            return; // Sem caminhos configurados
        }

        if (DialogueControl.instance != null && DialogueControl.instance.isShowing)
        {
            speed = 0f; // Para de se mover
            anim.SetBool("isWalking", false); // Animação de parado
        }
        else
        {
            speed = inicialSpeed; // Restaura a velocidade
            anim.SetBool("isWalking", true); // Animação de andando
        }

        // Move o NPC em direção ao ponto atual
        transform.position = Vector2.MoveTowards(transform.position, paths[index].position, speed * Time.deltaTime);

        // Se chegou perto do ponto atual, escolhe outro
        if (Vector2.Distance(transform.position, paths[index].position) < 0.1f)
        {
            if (index < paths.Count - 1)
            {
                // index++; // Caminho sequencial (desativado)
                index = Random.Range(0, paths.Count - 1); // Escolhe um caminho aleatório (menos o último)
            }
            else
            {
                index = 0; // Volta para o início
            }
        }

        // Define a direção do sprite (vira o NPC)
        Vector2 direction = paths[index].position - transform.position;

        if (direction.x > 0)
        {
            transform.eulerAngles = new Vector2(0, 0); // Olha para a direita
        }

        if (direction.x < 0)
        {
            transform.eulerAngles = new Vector2(0, 180); // Olha para a esquerda
        }
    }
}

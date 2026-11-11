using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC_Dialogue : MonoBehaviour
{

    public float dialogueRange; // Alcance em que o jogador pode interagir com o NPC   
    public LayerMask playerLayer; // Camada que representa o jogador (para detectar colisões apenas com o jogador)

    public DialogueSettings dialogue; // Referência para as configurações de diálogo deste NPC

    bool playerHit; // Flag que indica se o jogador está dentro da área de interação

    private List<string> sentences = new List<string>(); // Lista que armazena as frases do diálogo em formato de string
    private List<string> actorName = new List<string>();
    private List<Sprite> actorSprite = new List<Sprite>();


    void Start()
    {
        GetNPCInfo();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && playerHit)  // Verifica se o jogador apertou a tecla 'E' e está perto do NPC para iniciar o diálogo
        {
            // Chama o método que inicia o diálogo, passando as frases como array
            DialogueControl.instance.Speech(sentences.ToArray(), actorName.ToArray(), actorSprite.ToArray());
        }
    }

    // Método para coletar as frases em português do diálogo do NPC e armazená-las na lista
    void GetNPCInfo()
    {
        for (int i = 0; i < dialogue.dialogues.Count; i++)
        {


            switch (DialogueControl.instance.language)
            {
                case DialogueControl.idiom.pt:
                    // Adiciona cada frase em português à lista de sentenças
                    sentences.Add(dialogue.dialogues[i].sentences.portuguese);
                    break;
                case DialogueControl.idiom.eng:
                    // Adiciona cada frase em inglês à lista de sentenças
                    sentences.Add(dialogue.dialogues[i].sentences.english);
                    break;
                case DialogueControl.idiom.spa:
                    // Adiciona cada frase em espanhol à lista de sentenças
                    sentences.Add(dialogue.dialogues[i].sentences.spanish);
                    break;
            }

            actorName.Add(dialogue.dialogues[i].actorName);
            actorSprite.Add(dialogue.dialogues[i].profile);


            // Adiciona cada frase em português à lista de sentenças
            //  sentences.Add(dialogue.dialogues[i].sentences.portuguese);
        }
    }

    void FixedUpdate()
    {
        ShowDialogue();
    }

    // Verifica se há colisão do jogador dentro da área de diálogo
    void ShowDialogue()
    {
        // Cria o colisor ao redor do NPC para detectar o jogador
        Collider2D hit = Physics2D.OverlapCircle(transform.position, dialogueRange, playerLayer);

        // Se detectar o jogador, define playerHit como true
        if (hit != null)
        {
            playerHit = true;
        }
        else
        {
            playerHit = false;
        }
    }

    // Desenha um círculo visível no editor para mostrar o alcance do diálogo
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, dialogueRange);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Área de pegar água na beira do lago. Com a vara de pesca na mão, o E pesca aqui também.
public class Water : MonoBehaviour, IInteractable
{
    [SerializeField] private bool detectingPlayer; // Verifica se o jogador está na área de detecção
    [SerializeField] private int waterValue; // Quantidade de água que será adicionada

    public float FillValue => waterValue;

    private void OnEnable() => Interactables.Register(this);
    private void OnDisable() => Interactables.Unregister(this);

    #region Interação (E)

    public bool CanInteract(Player p) => detectingPlayer;

    public string GetPrompt(Player p) => LakeInteraction.Prompt(p, LakeInteraction.Action.Fill);

    public Vector2 InteractPoint(Player p) => p.transform.position;

    public void Interact(Player p) => LakeInteraction.Do(p, LakeInteraction.Action.Fill, waterValue);

    #endregion

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

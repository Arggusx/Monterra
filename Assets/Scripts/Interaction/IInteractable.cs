using System.Collections.Generic;
using UnityEngine;

// Tudo o que responde à tecla E (lago, canteiro, casa, lanterna, NPC) implementa esta interface.
// O objeto só diz SE pode interagir e O QUE acontece; quem lê a tecla e escolhe um único alvo
// é o PlayerInteractor. Assim dois objetos encostados nunca respondem ao mesmo aperto.
public interface IInteractable
{
    // O jogador está ao alcance e há algo para fazer agora?
    bool CanInteract(Player player);

    // Texto do indicador, sem acento (ex.: "Pescar", "Colher Trigo")
    string GetPrompt(Player player);

    // Ponto usado para escolher o alvo mais à frente do jogador e para posicionar o indicador.
    // Áreas grandes (beira do lago) devolvem a posição do próprio jogador.
    Vector2 InteractPoint(Player player);

    void Interact(Player player);
}

// Lista dos interativos ativos na cena. Cada um entra no OnEnable e sai no OnDisable.
public static class Interactables
{
    private static readonly List<IInteractable> all = new List<IInteractable>();

    public static IReadOnlyList<IInteractable> All => all;

    public static void Register(IInteractable interactable)
    {
        if (!all.Contains(interactable))
        {
            all.Add(interactable);
        }
    }

    public static void Unregister(IInteractable interactable) => all.Remove(interactable);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => all.Clear();
}

// Beira do lago: o que o E faz depende do que está na mão, e não mais de qual lado do lago o jogador está.
// Balde na mão enche; vara de pesca pesca; com outra coisa na mão vale a ação original daquela área.
public static class LakeInteraction
{
    public enum Action { Fish, Fill }

    public static Action Resolve(Player player, Action zoneDefault)
    {
        if (player.handlingObj == ToolIndex.Bucket) return Action.Fill;
        if (player.handlingObj == ToolIndex.Rod) return Action.Fish;
        return zoneDefault;
    }

    public static string Prompt(Player player, Action zoneDefault)
    {
        if (Resolve(player, zoneDefault) == Action.Fish)
        {
            return "Pescar";
        }

        PlayerItems items = player.GetComponent<PlayerItems>();
        return items.currentWater >= items.waterLimit ? "Balde cheio" : "Encher balde";
    }

    public static void Do(Player player, Action zoneDefault, float waterValue)
    {
        if (Resolve(player, zoneDefault) == Action.Fish)
        {
            player.GetComponent<PlayerAnim>().OnCastingStarded();
        }
        else
        {
            player.GetComponent<PlayerItems>().WaterLimit(waterValue);
        }
    }
}

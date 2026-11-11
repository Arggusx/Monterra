using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Cria automaticamente os sistemas do jogo (dia/noite, inventário, salvamento)
// em toda cena que tiver o Player. Assim não é preciso editar as cenas na mão.
public static class GameSystems
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Setup();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup();
    }

    private static void Setup()
    {
        FixEventSystem();

        if (Object.FindAnyObjectByType<Player>() == null)
        {
            return; // Cenas sem jogador (menus, testes) não precisam dos sistemas
        }

        if (Object.FindAnyObjectByType<DayNightCycle>() != null)
        {
            return; // Já foram criados nesta cena
        }

        GameObject systems = new GameObject("GameSystems");
        systems.AddComponent<DayNightCycle>(); // Primeiro: os outros usam o relógio
        systems.AddComponent<InventoryUI>();
        systems.AddComponent<SaveSystem>();
    }

    // O EventSystem antigo (StandaloneInputModule) usa o Input Manager, que foi desligado.
    // Troca pelo módulo do Input System para os botões da interface continuarem funcionando.
    private static void FixEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
        }

        StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            oldModule.enabled = false;
            Object.Destroy(oldModule);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            InputSystemUIInputModule module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions(); // Mouse, teclado e controle já configurados
        }
    }
}

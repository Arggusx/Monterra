using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Centraliza TODA a leitura de controles do jogo usando o novo Input System.
// Assim os outros scripts não precisam saber se o jogador está no teclado/mouse ou no controle.
//
// Teclado/mouse                          | Controle (Xbox / PlayStation)
// WASD / setas  -> andar                 | Analógico esquerdo / D-pad
// Botão esquerdo -> usar ferramenta      | X (Quadrado) ou RT (R2)
// Botão direito  -> rolar                | B (Bolinha)
// Shift esquerdo -> correr               | LT (L2) ou apertar o analógico esquerdo
// E              -> interagir            | A (X do PlayStation)
// F              -> comer peixe          | Y (Triângulo)
// G              -> comer cenoura        | (pelo inventário)
// I / Tab        -> inventário           | Select / Back (Share)
// 1 a 8 ou scroll -> barra rápida        | LB / RB (L1 / R1)
// F5 / F9        -> salvar / carregar    | -
public static class GameInput
{
    private static Keyboard Kb => Keyboard.current;
    private static Mouse Ms => Mouse.current;
    private static Gamepad Pad => Gamepad.current;

    // Evita que um clique na interface (inventário, diálogo) também use a ferramenta no mundo
    private static bool PointerOverUI =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    #region Movimento

    public static Vector2 Move
    {
        get
        {
            Vector2 dir = Vector2.zero;

            if (Kb != null)
            {
                if (Kb.aKey.isPressed || Kb.leftArrowKey.isPressed) dir.x -= 1f;
                if (Kb.dKey.isPressed || Kb.rightArrowKey.isPressed) dir.x += 1f;
                if (Kb.sKey.isPressed || Kb.downArrowKey.isPressed) dir.y -= 1f;
                if (Kb.wKey.isPressed || Kb.upArrowKey.isPressed) dir.y += 1f;
            }

            if (Pad != null)
            {
                Vector2 stick = Pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) // Zona morta para o analógico não "andar sozinho"
                {
                    dir += stick;
                }
                dir += Pad.dpad.ReadValue();
            }

            return new Vector2(Mathf.Clamp(dir.x, -1f, 1f), Mathf.Clamp(dir.y, -1f, 1f));
        }
    }

    public static bool RunHeld =>
        (Kb != null && Kb.leftShiftKey.isPressed) ||
        (Pad != null && (Pad.leftTrigger.ReadValue() > 0.5f || Pad.leftStickButton.isPressed));

    public static bool RunPressed =>
        (Kb != null && Kb.leftShiftKey.wasPressedThisFrame) ||
        (Pad != null && (Pad.leftTrigger.wasPressedThisFrame || Pad.leftStickButton.wasPressedThisFrame));

    #endregion

    #region Ações

    // Usar a ferramenta (cortar, cavar, regar, atacar)
    public static bool ActionPressed =>
        (Ms != null && Ms.leftButton.wasPressedThisFrame && !PointerOverUI) ||
        (Pad != null && (Pad.buttonWest.wasPressedThisFrame || Pad.rightTrigger.wasPressedThisFrame));

    public static bool ActionReleased =>
        (Ms != null && Ms.leftButton.wasReleasedThisFrame) ||
        (Pad != null && (Pad.buttonWest.wasReleasedThisFrame || Pad.rightTrigger.wasReleasedThisFrame));

    public static bool RollPressed =>
        (Ms != null && Ms.rightButton.wasPressedThisFrame && !PointerOverUI) ||
        (Pad != null && Pad.buttonEast.wasPressedThisFrame);

    public static bool RollReleased =>
        (Ms != null && Ms.rightButton.wasReleasedThisFrame) ||
        (Pad != null && Pad.buttonEast.wasReleasedThisFrame);

    // Interagir: pescar, coletar água, colher, construir, falar com NPC
    public static bool InteractPressed =>
        (Kb != null && Kb.eKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    // Rótulo do botão de interagir para o indicador na tela: "A" se o controle foi usado por último
    public static string InteractLabel =>
        Pad != null && (Kb == null || Pad.lastUpdateTime > Kb.lastUpdateTime) ? "A" : "E";

    public static bool EatFishPressed =>
        (Kb != null && Kb.fKey.wasPressedThisFrame) ||
        (Pad != null && Pad.buttonNorth.wasPressedThisFrame);

    public static bool EatCarrotPressed =>
        Kb != null && Kb.gKey.wasPressedThisFrame;

    public static bool InventoryPressed =>
        (Kb != null && (Kb.iKey.wasPressedThisFrame || Kb.tabKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.selectButton.wasPressedThisFrame);

    // Troca a semente escolhida para plantar
    public static bool NextSeedPressed =>
        (Kb != null && Kb.vKey.wasPressedThisFrame) ||
        (Pad != null && Pad.rightStickButton.wasPressedThisFrame);

    public static bool SavePressed => Kb != null && Kb.f5Key.wasPressedThisFrame;
    public static bool LoadPressed => Kb != null && Kb.f9Key.wasPressedThisFrame;

    // Atalho de teste que já existia: troca para a SceneTeste
    public static bool DebugScenePressed => Kb != null && Kb.pKey.wasPressedThisFrame;

    #endregion

    #region Barra rápida

    // Retorna o slot da barra rápida escolhido neste frame ou -1 se nada mudou
    public static int HotbarSelected(int current)
    {
        const int size = Inventory.HotbarSize;

        if (Kb != null)
        {
            if (Kb.digit1Key.wasPressedThisFrame) return 0;
            if (Kb.digit2Key.wasPressedThisFrame) return 1;
            if (Kb.digit3Key.wasPressedThisFrame) return 2;
            if (Kb.digit4Key.wasPressedThisFrame) return 3;
            if (Kb.digit5Key.wasPressedThisFrame) return 4;
            if (Kb.digit6Key.wasPressedThisFrame) return 5;
            if (Kb.digit7Key.wasPressedThisFrame) return 6;
            if (Kb.digit8Key.wasPressedThisFrame) return 7;
        }

        if (Ms != null)
        {
            float scroll = Ms.scroll.ReadValue().y; // Para cima = slot anterior, para baixo = próximo
            if (scroll > 0f) return (current + size - 1) % size;
            if (scroll < 0f) return (current + 1) % size;
        }

        if (Pad != null)
        {
            if (Pad.rightShoulder.wasPressedThisFrame) return (current + 1) % size;
            if (Pad.leftShoulder.wasPressedThisFrame) return (current + size - 1) % size;
        }

        return -1;
    }

    #endregion
}

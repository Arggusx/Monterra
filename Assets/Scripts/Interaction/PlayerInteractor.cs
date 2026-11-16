using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Único lugar que lê a tecla E para interagir com o mundo.
// A cada frame escolhe, entre os interativos ao alcance, o que está mais à frente do jogador
// (na direção em que ele andou por último), mostra o indicador flutuante nele e,
// ao apertar E, chama só esse. Fica no objeto do Player (colocado pelo GameSystems).
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Distância à frente do jogador usada para decidir qual objeto ele está encarando")]
    [SerializeField] private float lookAhead = 0.6f;
    [Tooltip("Altura do indicador acima do ponto de interação, em unidades do mundo")]
    [SerializeField] private float promptHeight = 1.1f;

    private Player player;
    private Vector2 facing = Vector2.down; // Última direção em que o jogador andou
    private bool wasBlocked;               // O jogador estava pausado ou morto no frame anterior

    private Camera cam;
    private RectTransform prompt;
    private TextMeshProUGUI promptText;
    private string shownText;

    public IInteractable Current { get; private set; }

    private void Start()
    {
        player = GetComponent<Player>();
        BuildPrompt();
    }

    private void OnDestroy()
    {
        if (prompt != null)
        {
            Destroy(prompt.transform.parent.gameObject); // O canvas do indicador
        }
    }

    private void Update()
    {
        // Diálogo, loja, inventário, construção: nada de interagir nem de indicador
        if (player.isPaused || player.isDead)
        {
            Current = null;
            wasBlocked = true;
            prompt.gameObject.SetActive(false);
            return;
        }

        if (player.direction.sqrMagnitude > 0.01f)
        {
            facing = player.direction.normalized;
        }

        Current = FindBest();

        // O aperto de E que fechou uma tela (diálogo, loja) não pode abrir outra coisa no mesmo frame
        if (!wasBlocked && Current != null && GameInput.InteractPressed)
        {
            Current.Interact(player);
        }
        wasBlocked = false;

        UpdatePrompt();
    }

    private IInteractable FindBest()
    {
        Vector2 position = player.transform.position;
        Vector2 probe = position + facing * lookAhead;

        IInteractable best = null;
        float bestScore = float.MaxValue;

        foreach (IInteractable candidate in Interactables.All)
        {
            if (!candidate.CanInteract(player))
            {
                continue;
            }

            float score = (candidate.InteractPoint(player) - probe).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    #region Indicador flutuante

    private void UpdatePrompt()
    {
        // O alvo pode ter mudado de estado ao interagir (ex.: canteiro recém-plantado)
        bool show = Current != null && !player.isPaused && Current.CanInteract(player);
        prompt.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        string text = $"<color=#FFD98C>{GameInput.InteractLabel}</color>  {UIFactory.Plain(Current.GetPrompt(player))}";
        if (text != shownText)
        {
            shownText = text;
            promptText.text = text;
        }

        if (cam == null)
        {
            cam = Camera.main;
        }
        if (cam != null)
        {
            Vector2 point = Current.InteractPoint(player);
            prompt.position = cam.WorldToScreenPoint(new Vector3(point.x, point.y + promptHeight, 0f));
        }
    }

    private void BuildPrompt()
    {
        // Abaixo da barra rápida (50) e das telas (100); não recebe clique
        Canvas canvas = UIFactory.CreateCanvas("Interaction_Canvas", 40, null);

        Image bg = UIFactory.CreateImage(canvas.transform, "Prompt", new Color(0.08f, 0.06f, 0.04f, 0.85f));
        bg.raycastTarget = false;
        prompt = bg.rectTransform;
        prompt.pivot = new Vector2(0.5f, 0f);

        HorizontalLayoutGroup layout = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 4, 4);
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = bg.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        promptText = UIFactory.CreateText(bg.transform, "Text", 18, TextAlignmentOptions.Center);
        promptText.richText = true;
        promptText.textWrappingMode = TextWrappingModes.NoWrap;

        prompt.gameObject.SetActive(false);
    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class Player : MonoBehaviour
{
    public bool isPaused;

    [SerializeField] private float speed; // Velocidade de movimento normal
    [SerializeField] private float runSpeed; // Velocidade ao correr
    [Header("Stats")] // Define no inspector da Unity
    public Image healthBarPlayer;
    public Image hungerBarPlayer;
    public Image staminBarPlayer;

    [Header("Currents")]
    public float currentHealthPlayer;
    public float currentStamina;
    public float currentHunger;

    [Header("Total")]
    public float totalHealthPlayer = 50f;
    public float maxStamina = 50f;
    public float maxHunger = 50f;

    [Header("Drain Stats")]
    public float hungerDrainPerSecond = 0.3f;
    public float staminaDrainPerSecond = 6f;
    public float healthDecayPerSecond = 0.5f;

    [Header("Regen Stats")]
    public float staminaRegenPerSecond = 3f;
    public float healthRegenPerSecond = 1f;

    [Header("Stamina das ferramentas (fração da stamina máxima por segundo de uso)")]
    [Range(0f, 1f)] public float cutStaminaPerSecond = 0.12f;    // Machado: ~8 s cortando sem parar
    [Range(0f, 1f)] public float digStaminaPerSecond = 0.12f;    // Pá
    [Range(0f, 1f)] public float attackStaminaPerSecond = 0.18f; // Espada: ~5,5 s atacando sem parar
    [Tooltip("Fração mínima de stamina para COMEÇAR a cortar, cavar ou atacar")]
    [Range(0f, 1f)] public float minStaminaToAct = 0.1f;

    [Header("Move")]
    public bool isRun = false;
    public bool isMoving = false;

    public bool isDead;

    public float valueHungry = 10f;

    [Header("Water")]
    public float waterUsePerSecond = 6f; // Água gasta por segundo ao regar (antes era 0.1 por frame = 6/s a 60 FPS)

    private Rigidbody2D rig; // Componente de física 2D
    private PlayerItems playerItems; // Script de itens do jogador
    private HUD_Controller hud_Controller;
    public Vector3 spawnPoint; // Ponto de renascimento
    private PlayerAnim playerAnim;
    private Animator anim;
    private Casting cast;


    private float initialSpeed; // Armazena a velocidade padrão
    private bool _isRunning; // Indica se o jogador está correndo
    private bool _isRolling; // Indica se o jogador está rolando
    private bool _isCutting; // Indica se o jogador está cortando
    private bool _isDigging; // Indica se o jogador está cavando
    private bool _isWatering; // Indica se o jogador está regando
    private bool _isAttacking;
    private bool _isCasting;


    private Vector2 _direction; // Direção da movimentação

    [HideInInspector] public int handlingObj; // Objeto atual que o jogador está segurando

    // Encapsulamentos para verificar estados externos
    public bool isCutting { get { return _isCutting; } set { _isCutting = value; } }
    public bool isRolling { get { return _isRolling; } set { _isRolling = value; } }
    public bool isRunning { get { return _isRunning; } set { _isRunning = value; } }
    public Vector2 direction { get { return _direction; } set { _direction = value; } }
    public bool isDigging { get { return _isDigging; } set { _isDigging = value; } }
    public bool isWatering { get { return _isWatering; } set { _isWatering = value; } }
    public bool isAttacking { get { return _isAttacking; } set { _isAttacking = value; } }
    public bool isCasting { get { return _isCasting; } set { _isCasting = value; } }
    public float isSpeed { get { return speed; } set { speed = value; } }
    public float isInicialSpeed { get { return initialSpeed; } set { initialSpeed = value; } }


    void OnEnable()
    {
        currentHealthPlayer = totalHealthPlayer;
        isDead = false;
        healthBarPlayer.fillAmount = 1f;
    }

    private void Start()
    {
        cast = FindAnyObjectByType<Casting>();
        currentHealthPlayer = totalHealthPlayer;
        rig = GetComponent<Rigidbody2D>(); // Acessa o Rigidbody do jogador
        playerItems = GetComponent<PlayerItems>(); // Acessa o script de itens
        hud_Controller = GetComponent<HUD_Controller>();
        initialSpeed = speed; // Salva a velocidade inicial
        spawnPoint = transform.position; // Posição inicial
        currentHealthPlayer = totalHealthPlayer;
        currentStamina = maxStamina;
        currentHunger = maxHunger;

    }

    public void Update()
    {
        if (isDead)
        {
            // Morto: não se move nem executa ações até renascer
            _direction = Vector2.zero;
            isMoving = false;
            _isRunning = false;
            speed = 0f;
            return;
        }

        if (!isPaused)
        {
            // Troca o slot da barra rápida (teclas 1 a 8, scroll do mouse ou LB/RB no controle)
            int slot = GameInput.HotbarSelected(playerItems.hotbarSlot);
            if (slot >= 0)
            {
                playerItems.SelectHotbar(slot);
            }

            // 0 machado, 1 pá, 2 balde, 3 espada, 4 vara de pesca; -1 se o item na mão não é ferramenta
            handlingObj = playerItems.SelectedTool;

            // Com comida na mão, o botão de usar come
            if (handlingObj < 0 && GameInput.ActionPressed)
            {
                EatFromSlot(playerItems.hotbarSlot);
            }

            if (GameInput.EatFishPressed)
            {
                foodFish();
            }

            if (GameInput.EatCarrotPressed)
            {
                foodCarrot();
            }




            // Chama os métodos relacionados a ações
            OnInput(); // Captura a direção de movimento
            OnRolling(); // Rolar
            OnRun(); // Correr
            OnCutting(); // Cortar
            OnDig(); // Cavar
            OnWatering(); // Regar
            OnAttacking();
            // OnCasting();

            HandleHunger();
            HandleStamina();
            HandleHealth();

            if (_direction.sqrMagnitude > 0.1f)
            {
                isMoving = true;
            }
            else
            {
                isMoving = false;
            }

            UpdateSpeed(); // Define a velocidade final do frame com base no estado atual

            if (GameInput.DebugScenePressed) // Se pressionar 'P', muda de cena
            {
                SceneManager.LoadScene("SceneTeste");
            }
        }
        else
        {
            // Pausado (diálogo, construção, pesca): zera a direção para não ficar com animação de andar
            _direction = Vector2.zero;
            isMoving = false;
            _isRunning = false;
        }
    }

    public void FixedUpdate()
    {
        if (!isPaused && !isDead)
        {
            OnMove(); // Aplica movimentação física
        }
    }

    #region Movement

    void OnInput()
    {
        _direction = GameInput.Move; // WASD, setas, analógico ou D-pad
    }

    void OnMove()
    {
        rig.MovePosition(rig.position + _direction * speed * Time.fixedDeltaTime); // Move o jogador
    }

    void OnRun()
    {
        if (GameInput.RunPressed) // Ao começar a correr
        {
            _isCutting = false; // Para de cortar se estiver correndo
            _isAttacking = false;
        }
    }

    // Centraliza o cálculo da velocidade para evitar estados inconsistentes
    // (ex.: animação de correr com velocidade de caminhada, ou parado após soltar o mouse)
    void UpdateSpeed()
    {
        bool isBusy = _isCutting || _isDigging || _isWatering || _isAttacking || _isCasting;
        bool wantsToRun = GameInput.RunHeld;

        _isRunning = wantsToRun && isMoving && !isBusy && currentStamina > 0f;

        if (isBusy)
        {
            speed = 0f;
        }
        else if (_isRunning)
        {
            speed = runSpeed;
        }
        else
        {
            speed = initialSpeed;
        }
    }

     void OnCasting()
    {
        if (handlingObj == 4) // Se estiver com espada
        {
            if (cast.detectingPlayer && GameInput.ActionPressed) // Se usar a ferramenta
            {
                speed = 0f; // Para de se mover
                _isRunning = false; // Para de correr
                _isCasting = true;
            }
            if (GameInput.ActionReleased)
            {
                speed = initialSpeed; // Volta a se mover
                _isCasting = false;
            }
        }
        else
        {
            isCasting = false;
        }
    }

    void OnAttacking()
    {
        if (handlingObj == 3) // Se estiver com espada
        {
            if (GameInput.ActionPressed && CanStartAction()) // Se usar a ferramenta
            {
                speed = 0f; // Para de se mover
                _isRunning = false; // Para de correr
                _isAttacking = true;
            }
            if (GameInput.ActionReleased)
            {
                speed = initialSpeed; // Volta a se mover
                _isAttacking = false;
            }
            if (_isAttacking && !SpendActionStamina(attackStaminaPerSecond))
            {
                _isAttacking = false; // Ficou sem fôlego no meio do golpe
            }
        }
        else
        {
            isAttacking = false;
        }
    }
    void OnCutting()
    {
        if (handlingObj == 0) // Se estiver com ferramenta de corte
        {
            if (GameInput.ActionPressed && CanStartAction()) // Se usar a ferramenta
            {
                _isCutting = true;
                speed = 0f; // Para de se mover
                _isRunning = false; // Para de correr
            }
            if (GameInput.ActionReleased)
            {
                _isCutting = false; // Não corta
                speed = initialSpeed; // Volta a se mover
            }
            if (_isCutting && !SpendActionStamina(cutStaminaPerSecond))
            {
                _isCutting = false; // Ficou sem fôlego
            }
        }
        else
        {
            isCutting = false;
        }
    }

    void OnRolling()
    {
        if (GameInput.RollPressed) // Botão direito do mouse ou B no controle
        {
            isRolling = true; // Pode usar 'Rolar'
        }
        if (GameInput.RollReleased) // Ao soltar, para de rolar
        {
            isRolling = false; // Não pode usar 'Rolar'
        }
    }


    void OnDig()
    {
        if (handlingObj == 1) // Se estiver com a pá
        {
            if (GameInput.ActionPressed && CanStartAction()) // Se usar a ferramenta
            {
                isDigging = true; // Começa a cavar
                speed = 0f; // Para de se mover
            }
            if (GameInput.ActionReleased) // Se soltou o botão
            {
                isDigging = false; // Para de cavar
                speed = initialSpeed; // Volta a se mover
            }
            if (isDigging && !SpendActionStamina(digStaminaPerSecond))
            {
                isDigging = false; // Ficou sem fôlego
            }
        }
        else
        {
            isDigging = false;
        }
    }

    void OnWatering()
    {
        if (handlingObj == 2) // Se o jogador estiver segurando o regador
        {
            // Se pressionar o botão esquerdo do mouse e ainda tiver água
            if (GameInput.ActionPressed && playerItems.currentWater > 0)
            {
                isWatering = true; // Começa a regar
                speed = 0f; // Para o movimento enquanto rega
            }

            // Se soltar o botão do mouse ou se acabar a água
            if (GameInput.ActionReleased || playerItems.currentWater <= 0)
            {
                isWatering = false; // Para de regar
                speed = initialSpeed; // Volta à velocidade inicial
            }

            // Se estiver regando(true), consome água
            if (isWatering)
            {
                // Diminui a água por segundo (independe do FPS), sem ficar negativa
                playerItems.currentWater = Mathf.Max(0f, playerItems.currentWater - waterUsePerSecond * Time.deltaTime);
            }
        }
        else
        {
            isWatering = false;
        }
    }

    // Atalhos de teclado (F e G). Quanto cada comida recupera está no ItemData do item.
    public void foodFish() => EatFirst(ItemIds.Fish);

    public void foodCarrot() => EatFirst(ItemIds.Crop(CropDatabase.Carrot));

    // Só deixa comer se o item recupera algo que não está cheio (não desperdiça comida)
    public bool CanEat(ItemData item)
    {
        if (item == null || !item.IsEdible || isDead)
        {
            return false;
        }

        return (item.hungerRestore > 0f && currentHunger < maxHunger)
            || (item.healthRestore > 0f && currentHealthPlayer < totalHealthPlayer)
            || (item.staminaRestore > 0f && currentStamina < maxStamina);
    }

    // Come uma unidade do item, tirando de qualquer slot do inventário
    public bool EatFirst(string itemId)
    {
        ItemData item = ItemDatabase.Get(itemId);
        if (!CanEat(item) || !playerItems.Spend(itemId))
        {
            return false;
        }

        ApplyFood(item);
        return true;
    }

    // Come uma unidade do slot clicado no inventário
    public bool EatFromSlot(int slot)
    {
        ItemData item = ItemDatabase.Get(playerItems.Inventory.GetSlot(slot).id);
        if (!CanEat(item) || playerItems.Inventory.RemoveAt(slot, 1) == 0)
        {
            return false;
        }

        ApplyFood(item);
        return true;
    }

    private void ApplyFood(ItemData item)
    {
        Comer(item.hungerRestore);
        currentHealthPlayer = Mathf.Clamp(currentHealthPlayer + item.healthRestore, 0f, totalHealthPlayer);
        currentStamina = Mathf.Clamp(currentStamina + item.staminaRestore, 0f, maxStamina);
    }

    void HandleHunger()
    {
        if (isMoving)
        {
            currentHunger -= hungerDrainPerSecond * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        }
    }

    // Cortando, cavando ou atacando (regar não cansa)
    private bool IsWorking => _isCutting || _isDigging || _isAttacking;

    // Precisa de um mínimo de fôlego para começar; sem isso avisa o jogador
    private bool CanStartAction()
    {
        if (currentStamina >= maxStamina * minStaminaToAct)
        {
            return true;
        }

        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.ShowMessage("Sem folego - descanse um pouco", 1.5f);
        }
        return false;
    }

    // Gasta stamina enquanto a ferramenta é usada. Retorna false quando ela acaba.
    private bool SpendActionStamina(float fractionPerSecond)
    {
        currentStamina = Mathf.Max(0f, currentStamina - maxStamina * fractionPerSecond * Time.deltaTime);
        return currentStamina > 0f;
    }

    void HandleStamina()
    {
        bool wantsToRun = GameInput.RunHeld;

        if (isRunning && currentStamina > 0)
        {
            currentStamina -= staminaDrainPerSecond * Time.deltaTime;
        }
        else if (!isRunning && !wantsToRun && !IsWorking)
        {
            currentStamina += staminaRegenPerSecond * Time.deltaTime; // Só recupera descansando
        }

        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
    }

    void HandleHealth()
    {
        if (currentHunger >= maxHunger / 1.5 && currentHealthPlayer < totalHealthPlayer)
        {
            currentHealthPlayer += healthRegenPerSecond * Time.deltaTime;
        }
        else if (currentHunger <= 0)
        {
            currentHealthPlayer -= healthDecayPerSecond * Time.deltaTime;
        }
        currentHealthPlayer = Mathf.Clamp(currentHealthPlayer, 0, totalHealthPlayer);
    }

    public void Comer(float valorFome)
    {
        currentHunger += valorFome;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
    }

    #endregion
}

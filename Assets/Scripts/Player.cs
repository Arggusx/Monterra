using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [Header("Water")]
    public float waterUsePerSecond = 6f; // Água gasta por segundo ao regar

    [Header("Regen Stats")]
    public float staminaRegenPerSecond = 3f;
    public float healthRegenPerSecond = 1f;

    [Header("Move")]
    public bool isRun = false;
    public bool isMoving = false;

    public bool isDead;

    public float valueHungryPerFish = 5f;
    public float valueHungry = 10f;

    private Rigidbody2D rig; // Componente de física 2D
    private PlayerItems playerItems; // Script de itens do jogador
    private HUD_Controller hud_Controller;
    public Vector3 spawnPoint; // Ponto de renascimento
    private PlayerAnim playerAnim;
    private Animator anim;


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
        if (!isPaused)
        {
            // Troca o item nas mãos com base na tecla pressionada
            if (Input.GetKeyDown(KeyCode.Q))
            {
                handlingObj = 0; // Ferramenta de corte
                Debug.Log("Pegou o machado");
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                handlingObj = 1; // Pá para cavar
                Debug.Log("Pegou a pá");
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                handlingObj = 2; // Regador
                Debug.Log("Pegou o balde");
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                handlingObj = 3; // Espada
                Debug.Log("Pegou a espada");
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                handlingObj = 4; // Vara de pesca
                Debug.Log("Pegou na vara");
            }
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                foodFish();
            }




            // Chama os métodos relacionados a ações
            OnInput(); // Captura a direção de movimento
            OnRolling(); // Rolar
            OnRun(); // Correr
            OnCutting(); // Cortar
            OnDig(); // Cavar
            OnWatering(); // Regar
            OnAttacking();

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

            bool wantsToRun = Input.GetKey(KeyCode.LeftShift);

            if (wantsToRun && currentStamina > 0 && isMoving)
            {
                isRunning = true;
            }

            if (wantsToRun && currentStamina == 0)
            {
                isRunning = false;
                speed = initialSpeed;

            }
        }
    }

    public void FixedUpdate()
    {
        if (!isPaused)
        {
            OnMove(); // Aplica movimentação física
        }
    }

    #region Movement

    void OnInput()
    {
        _direction = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // Direção com base nas teclas WASD ou setas
    }

    void OnMove()
    {
        rig.MovePosition(rig.position + _direction * speed * Time.fixedDeltaTime); // Move o jogador
    }

    void OnRun()
    {
        if (isDead)
        {
            speed = 0f;
            _isRunning = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) // Ao pressionar Shift
        {
            speed = runSpeed; // Aumenta a velocidade
            _isRunning = true;
            _isCutting = false; // Para de cortar se estiver correndo
            _isAttacking = false;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift)) // Ao soltar Shift
        {
            speed = initialSpeed; // Volta à velocidade normal
            _isRunning = false;
        }
    }

    void OnAttacking()
    {
        if (handlingObj == 3) // Se estiver com espada
        {
            if (Input.GetMouseButtonDown(0)) // Se clicar botão esquerdo do mouse
            {
                speed = 0f; // Para de se mover
                _isRunning = false; // Para de correr
                _isAttacking = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                speed = initialSpeed; // Volta a se mover
                _isAttacking = false;
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
            if (Input.GetMouseButtonDown(0)) // Se clicar botão esquerdo do mouse
            {
                _isCutting = true;
                speed = 0f; // Para de se mover
                _isRunning = false; // Para de correr
            }
            if (Input.GetMouseButtonUp(0))
            {
                _isCutting = false; // Não corta
                speed = initialSpeed; // Volta a se mover
            }
        }
        else
        {
            isCutting = false;
        }
    }

    void OnRolling()
    {
        if (Input.GetMouseButtonDown(1)) // Se clicar direito do mouse para iniciar rolagem
        {
            isRolling = true; // Pode usar 'Rolar'
        }
        if (Input.GetMouseButtonUp(1)) // Se soltar o botão direito do mouse para parar de rolar
        {
            isRolling = false; // Não pode usar 'Rolar'
        }
    }


    void OnDig()
    {
        if (handlingObj == 1) // Se estiver com a pá
        {
            if (Input.GetMouseButtonDown(0)) // Se clicou botão esquedo do mouse
            {
                isDigging = true; // Começa a cavar
                speed = 0f; // Para de se mover
            }
            if (Input.GetMouseButtonUp(0)) // Se soltou o botão esquerdo do mouse
            {
                isDigging = false; // Para de cavar
                speed = initialSpeed; // Volta a se mover
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
            if (Input.GetMouseButtonDown(0) && playerItems.currentWater > 0)
            {
                isWatering = true; // Começa a regar
                speed = 0f; // Para o movimento enquanto rega
            }

            // Se soltar o botão do mouse ou se acabar a água
            if (Input.GetMouseButtonUp(0) || (isWatering && playerItems.currentWater <= 0))
            {
                isWatering = false; // Para de regar
                speed = initialSpeed; // Volta à velocidade inicial
            }

            // Se estiver regando(true), consome água (por segundo, independente do FPS)
            if (isWatering)
            {
                playerItems.currentWater -= waterUsePerSecond * Time.deltaTime;
                playerItems.currentWater = Mathf.Max(playerItems.currentWater, 0f);
            }
        }
        else
        {
            isWatering = false;
        }
    }

    void foodFish()
    {
        if (playerItems.fishes > 0 && currentHunger < maxHunger)
        {
            playerItems.fishes--;
            Comer(valueHungryPerFish);
            Debug.Log("Comeu um peixe! Peixes restantes: " + playerItems.fishes);
        }
        else
        {
            Debug.Log("Não pode comer peixe (sem peixe ou fome cheia).");
        }
    }

    void HandleHunger()
    {
        if (isMoving)
        {
            currentHunger -= hungerDrainPerSecond * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        }
    }

    void HandleStamina()
    {
        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);

        if (isRunning && currentStamina > 0)
        {
            currentStamina -= staminaDrainPerSecond * Time.deltaTime;
        }
        else if (!isRunning && !wantsToRun)
        {
            currentStamina += staminaRegenPerSecond * Time.deltaTime;
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

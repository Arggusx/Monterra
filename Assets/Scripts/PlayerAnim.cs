using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnim : MonoBehaviour
{
    [Header("Attacking Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float radius;
    [SerializeField] private LayerMask enemyLayer;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Quaternion lastRotation;
    private bool isDead;
    private bool isHitting;
    private float timeCount;
    private float recoveryTime = 1.5f;
    private Player player; // Referência ao script Player
    private Animator anim; // Referência ao componente Animator
    private Casting cast;

    void Start()
    {
        player = GetComponent<Player>(); // Pega o componente Player
        anim = GetComponent<Animator>(); // Pega o componente Animator
        startPosition = transform.position;
        startRotation = transform.rotation;

        cast = FindAnyObjectByType<Casting>();
    }

    void Update()
    {
        if (player.isDead)
        {
            transform.rotation = lastRotation; // ← Impede a rotação após a morte
            return; // Impede que OnMove/OnRun alterem a rotação
        }

        // Morte por fome (vida chegou a zero sem ser por ataque)
        if (player.currentHealthPlayer <= 0)
        {
            Die();
            return;
        }

        OnMove(); // Chama a função de movimentação
        OnRun();  // Chama a função de corrida

        if (isHitting)
        {
            timeCount += Time.deltaTime; // Contador
        }

        if (timeCount >= recoveryTime)
        {
            isHitting = false; // Toma mais um dano
            timeCount = 0f; // Zera para reiniciar a contágem
        }
    }

    #region Movement

    void OnMove()
    {
        if (player.direction.sqrMagnitude > 0) // Verifica se está se movendo (Vector2 diferente de zero)
        {
            if (player.isRolling) // Se estiver rolando
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("roll")) // Se a animação não estiver sendo executada
                {
                    anim.SetTrigger("isRoll"); // Aciona animação de rolar
                }
            }
            else
            {
                anim.SetInteger("transition", 1); // Animação de andar
            }
        }
        else
        {
            anim.SetInteger("transition", 0); // Animação de idle (parado)
        }

        if (player.direction.x > 0) // Indo para a direita
        {
            transform.eulerAngles = new Vector2(0, 0); // Mantém a rotação normal
        }

        if (player.direction.x < 0) // Indo para a esquerda
        {
            transform.eulerAngles = new Vector2(0, 180); // Espelha horizontalmente
        }

        if (player.isCutting) // Se estiver cortando
        {
            anim.SetInteger("transition", 3); // Animação de cortar
        }

        if (player.isDigging) // Se estiver cavando
        {
            anim.SetInteger("transition", 4); // Animação de cavar
        }

        if (player.isWatering) // Se estiver regando
        {
            anim.SetInteger("transition", 5); // Animação de regar
        }

        if (player.isAttacking)
        {
            anim.SetInteger("transition", 6);
        }
    }

    void OnRun()
    {
        if (player.isRunning && player.direction.sqrMagnitude > 0) // Se estiver correndo
        {
            anim.SetInteger("transition", 2); // Animação de correr
        }
    }
    #endregion

    #region Attack

    public void OnAttack()
    {
        // Armazena a posição do colisor da espada, raio e a layer de quem vai ser atacado
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, radius, enemyLayer);

        if (hit != null)
        {
            AnimationControll enemy = hit.gameObject.GetComponentInChildren<AnimationControll>(); // Pega elementos do objeto filho
            if (enemy != null)
            {
                enemy.OnHit();
                Debug.Log("Atacou o inimigo");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, radius);
        }
    }

    #endregion

    // Quando o jogador clica no botão referente a pesca
    public void OnCastingStarded()
    {
        anim.SetTrigger("isCasting");
        player.isPaused = true;
    }

    // Chamado quando a animação de pesca termina
    public void OnCastingEnded()
    {
        if (cast != null)
        {
            cast.OnCasting();
        }
        player.isPaused = false;
    }

    public void OnHammeringStarted() // Ao começar a contágem de construção, começa a animação
    {
        anim.SetBool("hammering", true);
    }

    public void OnHammeringEnded() // Ao terminar de construir, para a animação
    {
        anim.SetBool("hammering", false);
    }

    public void OnHit()
    {
        // Não toma dano se já estiver morto ou ainda no tempo de recuperação do último golpe
        if (player.isDead || isHitting)
        {
            return;
        }

        isHitting = true; // Inicia o tempo de invencibilidade (recoveryTime)
        timeCount = 0f;

        anim.SetTrigger("hit"); //Animação de hit
        player.currentHealthPlayer -= 3; // Aplica dano ao player

        if (player.currentHealthPlayer <= 0)  // Se tiver com menos de 1 de vida (morto)
        {
            Die();
        }
    }

    private void Die()
    {
        if (player.isDead)
        {
            return;
        }

        anim.SetTrigger("death"); // Toca a animação de morte
        player.isDead = true;
        player.isPaused = false; // Garante que não fique travado caso morra durante uma ação
        lastRotation = transform.rotation; // Não permite rotação
        StartCoroutine(DieCoroutine()); // Aguarda a animação antes de renascer
    }

    private IEnumerator DieCoroutine()
    {
        // Impede movimento durante a morte

        player.direction = Vector2.zero;
        player.isRunning = false;
        player.isAttacking = false;

        yield return new WaitForSeconds(2f); // Tempo da animação de morte

        Respawn();
    }

    void Respawn()
    {
        // Reposiciona e reseta rotação
        player.transform.position = startPosition;
        player.transform.rotation = startRotation;

        // Restaura a vida, a fome e a stamina (senão morreria de fome de novo logo ao renascer)
        player.currentHealthPlayer = player.totalHealthPlayer;
        player.currentHunger = player.maxHunger;
        player.currentStamina = player.maxStamina;
        isHitting = false;
        timeCount = 0f;
        player.healthBarPlayer.fillAmount = 1f;
        player.isDead = false;
        player.isSpeed = player.isInicialSpeed;

        // Reinicia os triggers e define idle
        anim.ResetTrigger("death");
        anim.ResetTrigger("hit"); // se tiver esse
        anim.SetTrigger("respawn"); // Precisa estar ligado ao retorno ao estado Idle

        anim.SetInteger("transition", 0); // Seta animação de idle ao recascer

        player.gameObject.SetActive(true); // Reativa o objeto do player
        player.transform.rotation = startRotation; // Volta a rotacionar
    }


}

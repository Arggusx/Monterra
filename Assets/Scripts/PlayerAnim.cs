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
    private bool hasPlayedDeathAnim = false;
    private float timeCount;
    private float recoveryTime = 1.5f;
    private Player player; // Referência ao script Player
    private Animator anim; // Referência ao componente Animator
    private bool isAttacking;

    private Skeleton skeleton;

    private Casting cast;

    void Start()
    {
        skeleton = GetComponentInParent<Skeleton>();
        player = GetComponent<Player>(); // Pega o componente Player
        anim = GetComponent<Animator>(); // Pega o componente Animator
        startPosition = transform.position;
        startRotation = transform.rotation;

        cast = FindObjectOfType<Casting>();
    }

    void Update()
    {
        if (player.isDead)
        {
            transform.rotation = lastRotation; // ← Impede a rotação após a morte
            return; // Impede que OnMove/OnRun alterem a rotação
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
            hit.gameObject.GetComponentInChildren<AnimationControll>().OnHit(); // Pega elementos do objeto filho
            Debug.Log("Atacou o inimigo");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(attackPoint.position, radius);
    }

    #endregion

    public void OnCasting()
    {
        cast.OnCasting();
    }

    // Quando o jogador clica no botão referente a pesca
    public void OnCastingStarded()
    {
        anim.SetTrigger("isCasting");
        player.isPaused = true;
    }

    // Chamado quando a animação de pesca termina
    public void OnCastingEnded()
    {
        cast.OnCasting();
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
        if (isHitting || player.isDead) return; // Invencível durante o tempo de recuperação

        isHitting = true;
        anim.SetTrigger("hit"); //Animação de hit
        player.currentHealthPlayer -= 3; // Aplica dano ao player

        if (player.currentHealthPlayer <= 0 && !player.isDead)  // Se tiver com menos de 1 de vida (morto)
        {
            anim.SetTrigger("death"); // Toca a animação de morte
            player.isDead = true;
            lastRotation = transform.rotation; // Não permite rotação
            StartCoroutine(DieCoroutine()); // Aguarda a animação antes de desativar

        }

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

        // Restaura a vida
        player.currentHealthPlayer = player.totalHealthPlayer;
        player.healthBarPlayer.fillAmount = 1f;
        player.isDead = false;
        player.isSpeed = player.isInicialSpeed;
        isHitting = false;
        timeCount = 0f;

        // Reinicia os triggers e define idle
        anim.ResetTrigger("death");
        anim.ResetTrigger("hit"); // se tiver esse
        anim.SetTrigger("respawn"); // Precisa estar ligado ao retorno ao estado Idle

        anim.SetInteger("transition", 0); // Seta animação de idle ao recascer

        player.gameObject.SetActive(true); // Reativa o objeto do player
        player.transform.rotation = startRotation; // Volta a rotacionar
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationControll : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float radius;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int xpValue;

    private PlayerAnim playerAnim;
    private Animator anim;
    private Skeleton skeleton;
    private PlayerItems playerItems;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        playerItems = FindAnyObjectByType<PlayerItems>();
        anim = GetComponent<Animator>();
        playerAnim = FindAnyObjectByType<PlayerAnim>();
        skeleton = GetComponentInParent<Skeleton>(); // Pega elementos do objeto pai
        // Guarda a posição do objeto PAI (o esqueleto), que é quem será reposicionado ao renascer
        startPosition = skeleton.transform.position;
        startRotation = skeleton.transform.rotation;
        skeleton.currentHealth = skeleton.totalHealth;
    }

    public void PlayAnim(int value)
    {
        anim.SetInteger("transition", value);
    }

    public void Attack()
    {
        if (!skeleton.isDead)
        {
            Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, radius, playerLayer); // Detecção de colisão somente ao bater no player

            if (hit != null && playerAnim != null)
            {
                playerAnim.OnHit();
                Debug.Log("Player tomou dano");
            }
            else
            {
                Debug.Log("Inimigo errou o ataque");
            }
        }
    }

    public void OnHit()
    {
        if (skeleton.isDead)
        {
            return; // Não apanha depois de morto
        }

        anim.SetTrigger("hit");
        skeleton.currentHealth -= 3; // Dano do player
        skeleton.helthBar.fillAmount = skeleton.currentHealth / skeleton.totalHealth; // Barra de vida dinâmica
        if (skeleton.currentHealth <= 0 && !skeleton.isDead) // Verifica se realmente morreu
        {
            anim.SetTrigger("death"); // Toca a animação de morte
            skeleton.isDead = true;
            playerItems.XpLimit(xpValue > 0 ? xpValue : 10); // XP que o player recebe (definido no Inspector)
            StartCoroutine(DieCoroutine()); // Aguarda a animação antes de desativar
        }
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(1.5f); // Tempo da animação de morte
        Die();
    }

    void Die()
    {
        // Este script fica num filho do esqueleto: ao desativar o pai, Invoke/Coroutines daqui param.
        // Por isso o renascimento é agendado num objeto que continua ativo.
        RespawnScheduler.Schedule(3f, Respawn); // Renasce após 3 segundos
        skeleton.gameObject.SetActive(false); // Desativa o inimigo
    }

    void Respawn()
    {
        // Restaura o movimento após remover
        skeleton.transform.position = startPosition;
        skeleton.transform.rotation = startRotation;

        // Restaura a vida
        skeleton.currentHealth = skeleton.totalHealth;

        skeleton.isDead = false;
        skeleton.helthBar.fillAmount = 1f; // Restaura a barra de vida

        skeleton.gameObject.SetActive(true); // Reativa antes de mexer no Animator (Animator inativo ignora parâmetros)

        anim.ResetTrigger("death");
        anim.ResetTrigger("hit");
        anim.SetInteger("transition", 0);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.DrawWireSphere(attackPoint.position, radius);
        }
    }
}

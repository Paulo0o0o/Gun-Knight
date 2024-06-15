using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 11f;
    public float currentHealth;
    public float speed = 2f;
    public Transform groundDetection;
    public Transform player;
    public float detectionRange = 5f;
    public float fovAngle = 45f;
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    public float chaseDuration = 2f;
    public float damageAmount = 1f;
    public float damageInterval = 1f;
    public float capsuleHeight = 2f; // Capsule height
    public float moveDirection = 1f;
    public float stoppingDistance = 0.5f;
    public CapsuleCollider2D capsuleCollider;
    public bool resetAttackCooldown = true;
    public bool isDead = false;
    public PlayerController playerScript;
    public bool attackFrame = false;

    Animator anim;
    Rigidbody2D rb;
    Transform t;

    private bool movingRight = true;
    private bool isChasing = false;
    private Coroutine stopChaseCoroutine;
    private Coroutine stopDamage;
    private bool isPlayerInsideCapsule = false;

    void Start()
    {
        currentHealth = maxHealth;
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerScript = player.GetComponent<PlayerController>();
        t = transform;
    }

    void Update()
    {
        if (currentHealth <= 0)
        {
            isDead = true;
            anim.SetBool("isDead", true);
            //Destroy(gameObject);
        }

        CheckCapsuleCast();
    }

    private void FixedUpdate()
    {
        if (!isDead)
        {
            if (!isChasing)
            {
                Patrol();
                CheckForPlayer();
            }
            else
            {
                FollowPlayer();
            }
        }
    }

    void Patrol()
    {
        rb.velocity = new Vector2((moveDirection) * speed, rb.velocity.y);

        RaycastHit2D groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, 1f, groundLayer);
        if (groundInfo.collider == false)
        {
            if (movingRight)
            {
                moveDirection = -1;
                t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                movingRight = false;
            }
            else
            {
                moveDirection = 1;
                t.localScale = new Vector3(Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                movingRight = true;
            }
        }
    }

    void CheckForPlayer()
    {
        Vector2 direction = movingRight ? Vector2.right : Vector2.left;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Vector2 directionToPlayer = (hit.transform.position - transform.position).normalized;
                float angleToPlayer = Vector2.Angle(direction, directionToPlayer);

                if (angleToPlayer < fovAngle / 2)
                {
                    isChasing = true;
                    if (stopChaseCoroutine != null)
                    {
                        StopCoroutine(stopChaseCoroutine);
                    }
                    return;
                }
            }
        }
    }

    void FollowPlayer()
    {
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            Vector2 distance = (player.position - transform.position);
            if((distance.x * moveDirection) > stoppingDistance)
            {
                rb.velocity = new Vector2(direction.x, 0) * speed;
            }
            else
            {
                rb.velocity = new Vector2(0, 0);
            }

            if (player.position.x > transform.position.x && !movingRight)
            {
                Flip();
            }
            else if (player.position.x < transform.position.x && movingRight)
            {
                Flip();
            }

            RaycastHit2D groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, 1f);
            if (groundInfo.collider == false)
            {
                isChasing = false;
                if (movingRight)
                {
                    moveDirection = -1;
                    t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                    movingRight = false;
                }
                else
                {
                    moveDirection = 1;
                    t.localScale = new Vector3(Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
                    movingRight = true;
                }
            }

            if (Vector2.Distance(transform.position, player.position) > detectionRange)
            {
                if (stopChaseCoroutine == null)
                {
                    stopChaseCoroutine = StartCoroutine(StopChaseAfterDelay());
                }
            }
        }
    }

    IEnumerator StopChaseAfterDelay()
    {
        yield return new WaitForSeconds(chaseDuration);
        isChasing = false;
        stopChaseCoroutine = null;
    }

    void Flip()
    {
        if (movingRight)
        {
            moveDirection = -1;
            t.localScale = new Vector3(-Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
            movingRight = false;
        }
        else
        {
            moveDirection = 1;
            t.localScale = new Vector3(Mathf.Abs(t.localScale.x), t.localScale.y, t.localScale.z);
            movingRight = true;
        }
    }

    void CheckCapsuleCast()
    {
        Vector2 capsulePos = new Vector2(transform.position.x + capsuleCollider.offset.x, transform.position.y + capsuleCollider.offset.y);
        Vector2 capsuleSize = new Vector2(capsuleCollider.size.x, capsuleCollider.size.y);

        Collider2D hit = Physics2D.OverlapCapsule(capsulePos, capsuleSize, CapsuleDirection2D.Vertical, 0f, playerLayer);
        if(hit != null && hit.gameObject.tag == "Player" && !playerScript.isDead && !isDead)
        {
            isPlayerInsideCapsule = true;
            if(movingRight && player.transform.position.x < transform.position.x)
            {
                Flip();
            }
            else if(!movingRight && player.transform.position.x > transform.position.x)
            {
                Flip();
            }
            if (resetAttackCooldown && !anim.GetBool("gettingHurt") && !anim.GetBool("doAttack"))
            {
                anim.SetTrigger("doAttack");
            }
        }
        else
        {
            isPlayerInsideCapsule=false;
        }
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(damageInterval);
        anim.SetBool("resetAttackCooldown", true);
        resetAttackCooldown = true;
    }

    public void DamagePlayer()
    {
        if(isPlayerInsideCapsule && resetAttackCooldown)
        {
            if (player != null)
            {
                playerScript.TakeDamage(damageAmount);
                anim.SetBool("resetAttackCooldown", false);
                resetAttackCooldown = false;
                StartCoroutine(AttackCooldown());
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 fovLine1 = Quaternion.AngleAxis(fovAngle / 2, Vector3.forward) * transform.right * detectionRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fovAngle / 2, Vector3.forward) * transform.right * detectionRange;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }

    public void TakeDamage(float damage)
    {
        if (!isDead)
        {
            currentHealth -= damage;
            anim.SetBool("gettingHurt", true);
            Debug.Log("Enemy took damage: " + damage + ". Current health: " + currentHealth);
        }
    }

    public void StopGettingHurt()
    {
        anim.SetBool("gettingHurt", false);
    }

    public void quitAttackTrigger()
    {
        anim.ResetTrigger("doAttack");
    }

}

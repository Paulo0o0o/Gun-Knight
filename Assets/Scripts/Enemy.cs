using Cinemachine;
using System.Collections;
//using UnityEditor.Experimental.GraphView;
using TMPro;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float currentHealth;
    public float fovAngle = 45f;
    public float moveDirection = 1f;
    public float detectionRange = 5f;
    public bool resetAttackCooldown = true;
    public bool isDead = false;
    public bool attackFrame = false;
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    public Transform groundDetection;
    public Transform wallDetection;
    public Transform player;
    public CapsuleCollider2D capsuleCollider;
    public PlayerController playerScript;
    public Coroutine stopChaseCoroutine;
    public GameObject damagePopUpObj;
    public TMP_Text damagePopUpTxt;

    protected float maxHealth = 10f;
    protected float speed = 2f;
    protected float damageAmount = 1f;
    protected float damageInterval = 1f;
    protected float capsuleHeight = 2f;
    protected float stoppingDistance = 0.5f;
    protected float retreatDistance = 5f;
    protected float chaseDuration = 3.5f;
    protected bool movingRight = true;
    protected bool isChasing = false;
    protected bool isPlayerInsideCapsule = false;
    protected Rigidbody2D rb;
    protected Transform t;
    protected Animator anim;
    protected Coroutine stopDamage;
    protected CinemachineImpulseSource impulseSource;

    [SerializeField] private AudioClip damageSoundClip;
    [SerializeField] private AudioClip dieSoundClip;

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
            PlayDeathSound();
            isDead = true;
            anim.SetBool("isDead", true);
        }
        if(!isDead && !playerScript.isDead)
        {
            CheckCapsuleCast();
        }
    }

    private void FixedUpdate()
    {
        if (!isDead && !playerScript.isDead)
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

    protected void Patrol()
    {
        rb.velocity = new Vector2((moveDirection) * speed, rb.velocity.y);

        RaycastHit2D groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, 0.5f, groundLayer);
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

        RaycastHit2D wallInfo = Physics2D.Raycast(wallDetection.position, Vector2.right, 0.1f, groundLayer);
        if (wallInfo.collider == true)
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

    protected void CheckForPlayer()
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

    protected void FollowPlayer()
    {
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            Vector2 distance = (player.position - transform.position);
            if((distance.x * moveDirection) > stoppingDistance)
            {
                anim.SetBool("isIdle", false);
                rb.velocity = new Vector2(direction.x, rb.velocity.y) * speed;
            }
            else
            {
                anim.SetBool("isIdle", true);
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

            RaycastHit2D groundInfo = Physics2D.Raycast(groundDetection.position, Vector2.down, 1f, groundLayer);
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

            RaycastHit2D wallInfo = Physics2D.Raycast(wallDetection.position, Vector2.right, 0.1f, groundLayer);
            if (wallInfo.collider == true)
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

    protected void Flip()
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

    protected void CheckCapsuleCast()
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

    /*void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 fovLine1 = Quaternion.AngleAxis(fovAngle / 2, Vector3.forward) * transform.right * detectionRange;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fovAngle / 2, Vector3.forward) * transform.right * detectionRange;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }*/

    public void TakeDamage(float damage)
    {
        if (!isDead)
        {
            currentHealth -= damage;
            anim.SetBool("gettingHurt", true);
            SoundFXManager.instance.PlaySoundFXCLip(damageSoundClip, transform, 0.7f);
            CameraShake.instance.ShakeCamera(impulseSource, damage);
            damagePopUpTxt.text = (Mathf.Round(damage * 10) * 0.1f).ToString();
            GameObject newDamageTxt = Instantiate(damagePopUpObj, transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
            newDamageTxt.GetComponent<Rigidbody2D>().AddForce(new Vector2(Random.Range(-20, 20), 40));
            isChasing = true;
            FollowPlayer();
            //Debug.Log("Enemy took damage: " + damage + ". Current health: " + currentHealth);
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

    public void PlayDeathSound()
    {
        if (!isDead)
        {
            SoundFXManager.instance.PlaySoundFXCLip(dieSoundClip, transform, 0.5f);
        }
    }
}

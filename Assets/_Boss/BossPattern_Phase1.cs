using UnityEngine;
using System.Collections;

public class BossPattern_Phase1 : MonoBehaviour
{
    [Header("Attack Settings")]
    public float warningDuration = 0.5f; // 빨간색 경고가 보여지는 시간
    public float attackDuration = 0.25f;  // 실제 공격 판정이 남아있는 시간

    [Header("Warning/Attack Zones (Assign in Inspector)")]
    public GameObject highAttackZone; // 상단 공격 범위 (서있으면 맞음)
    public GameObject lowAttackZone;  // 하단 공격 범위 (땅에 있으면 맞음)

    void Start()
    {
        highAttackZone.SetActive(false);
        lowAttackZone.SetActive(false);
    }

    // 상단 공격 (숙여서 피해야 함)
    public void ExecuteHighAttack(BossPatternManager manager)
    {
        StartCoroutine(AttackRoutine(highAttackZone, manager));
    }
    // 하단 공격 (점프로 피해야 함)
    public void ExecuteLowAttack(BossPatternManager manager)
    {
        StartCoroutine(AttackRoutine(lowAttackZone, manager));
    }

    IEnumerator AttackRoutine(GameObject attackZone, BossPatternManager manager)
    {
        FindObjectOfType<QAAnalyzer>()?.OnAttackStarted();

        manager.isAttacking = true;
        attackZone.SetActive(true);

        Collider2D col = attackZone.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SpriteRenderer sr = attackZone.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0f, 0f, 0.3f);

        yield return new WaitForSeconds(warningDuration);
        if (sr != null) sr.color = new Color(1f, 0f, 0f, 1f);
        if (col != null) col.enabled = true;
        yield return new WaitForSeconds(attackDuration);

        attackZone.SetActive(false);
        if (col != null) col.enabled = false;
        manager.isAttacking = false;
    }
}

using UnityEngine;
using Unity.MLAgents; // 나중에 ML-Agents 연결할 때 주석 해제

public class BossPatternManager : MonoBehaviour
{
    [Header("Curriculum Settings")]
    // 1: 점프/숙이기, 2: 대쉬/거리유지, 3: 궁극기(특정 좌표)
    public int currentLesson = 1;

    [Header("Pattern Scripts")]
    public BossPattern_Phase1 phase1Pattern;
    // public BossPattern_Phase2 phase2Pattern; // 나중에 추가
    // public BossPattern_Phase3 phase3Pattern; // 나중에 추가

    [Header("Movement Settings")]
    public Transform player;       // 쫓아갈 플레이어 지정
    public float moveSpeed = 0.5f; // 보스의 이동 속도 (느리게 설정)
    private float startY = -3.5f;
    public bool isAttacking = false; // 공격 중인지 체크

    private float patternTimer = 0f;
    public float patternCooldown = 3f; // 패턴 사이의 간격

    void Start()
    {
        startY = transform.position.y;

    }
    void Update()
    {
        // 나중에 ML-Agents의 Academy.Instance.EnvironmentParameters.GetWithDefault("lesson_level", 1); 
        // 등을 통해 currentLesson 값을 동적으로 업데이트하게 됩니다.

        if (player != null && !isAttacking)
        {
            Vector3 targetPosition = new Vector3(player.position.x, startY, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }

        patternTimer += Time.deltaTime;

        if (patternTimer >= patternCooldown)
        {
            ExecuteRandomPatternForCurrentLesson();
            patternTimer = 0f; // 타이머 초기화
        }
    }

    void ExecuteRandomPatternForCurrentLesson()
    {
        switch (currentLesson)
        {
            case 1:
                // 1단계: 하단(점프 요구) 또는 상단(숙이기 요구) 공격 무작위 실행
                bool isHighAttack = Random.value > 0.5f;
                if (isHighAttack)
                {
                    phase1Pattern.ExecuteHighAttack(this); // 숙여서 피하는 공격
                }
                else
                {
                    phase1Pattern.ExecuteLowAttack(this);  // 점프로 피하는 공격
                }
                break;

            case 2:
                // 2단계 로직 (대쉬 공격, 기절 장판 등)
                break;

            case 3:
                // 3단계 로직 (궁극기)
                break;
        }
    }
}

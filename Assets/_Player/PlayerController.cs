using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerController : Agent
{
    public enum PersonaType { Beginner, Intermediate, Expert }
    [Header("Persona Settings")]
    public PersonaType currentPersona = PersonaType.Expert;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f;
    public float jumpForce = 5f;
    public float dashForce = 7.5f;
    public float dashDuration = 0.2f;
    private Vector3 startPos;

    [Header("State Check")]
    public bool isGrounded;
    public bool isDucking;
    public bool isDashing;

    [Header("Grounded Settings")]
    public LayerMask groundLayer; // Floor 레이어
    public float rayDistance = 0.1f;

    private int jumpCount = 0;
    private int maxJumps = 2; // 2단 점프
    private bool isJumpCooldown = false;
    private float jumpCooldown = 0.2f; // 점프 후 0.1초 동안은 다시 점프 불가
    private float lastJumpTime;

    [Header("Dash Settings")]
    public int maxAirDashes = 1; // 공중에서 허용할 대쉬 횟수
    private int airDashCount = 0;
    public float dashCooldown = 0.3f;
    private bool isDashCooldown = false;
    private float facingDir = 1f; // 플레이어가 바라보는 방향 (1: 오른쪽, -1: 왼쪽)

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private Vector3 originalScale;

    [Header("Boss & Environment References")]
    public Transform bossTransform;
    // 1단계 패턴
    public GameObject highAttackZone;
    public GameObject lowAttackZone;
    // 2단계
    public GameObject dashAttackZone;
    public GameObject stunZone;
    public GameObject bossScoringZone;
    public float timeInStunZone = 0f;
    // 3단계 패턴
    public GameObject[] ultimateZones = new GameObject[5];
    // 반응 속도
    private float highTimer = 0f;
    private float lowTimer = 0f;
    private float dashTimer = 0f;
    private float stunTimer = 0f;
    private float[] ultTimers = new float[5];

    private bool jumpRequested = false;
    private bool dashRequested = false;
    private int prevJumpAction = 0;
    private int prevDashAction = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        originalSize = col.size;
        originalOffset = col.offset;
        originalScale = transform.localScale;
        startPos = transform.localPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            jumpRequested = true;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Q))
            dashRequested = true;
    }

    void FixedUpdate()
    {
        Vector2 rayStart = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 0.15f, groundLayer);
        RaycastHit2D ceilingHit = Physics2D.Raycast(new Vector2(col.bounds.center.x, col.bounds.max.y), Vector2.up, 0.1f);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Floor"))
            {
                isGrounded = true;
                jumpCount = 0;
                airDashCount = 0;
            }
        }
        else
        {
            isGrounded = false;
        }

        if (ceilingHit.collider != null)
        {
            AddReward(-1.0f);
            EndEpisode();
        }

        if (Mathf.Abs(transform.localPosition.x) > 20f || transform.localPosition.y < -10f)
        {
            AddReward(-1.0f);
            EndEpisode();
        }

        bool isAnyUltActive = false;
        if (ultimateZones != null)
        {
            for (int i = 0; i < ultimateZones.Length; i++)
            {
                if (ultimateZones[i] != null && ultimateZones[i].activeSelf)
                {
                    isAnyUltActive = true;
                    break;
                }
            }
        }

        highTimer = (highAttackZone != null && highAttackZone.activeSelf) ? highTimer + Time.fixedDeltaTime : 0f;
        lowTimer = (lowAttackZone != null && lowAttackZone.activeSelf) ? lowTimer + Time.fixedDeltaTime : 0f;
        dashTimer = (dashAttackZone != null && dashAttackZone.activeSelf) ? dashTimer + Time.fixedDeltaTime : 0f;
        stunTimer = (stunZone != null && stunZone.activeSelf) ? stunTimer + Time.fixedDeltaTime : 0f;

        for (int i = 0; i < 5; i++)
        {
            if (ultimateZones != null && i < ultimateZones.Length && ultimateZones[i] != null && ultimateZones[i].activeSelf)
            {
                ultTimers[i] += Time.fixedDeltaTime;
                isAnyUltActive = true;
            }
            else
            {
                ultTimers[i] = 0f;
            }
        }

        // 생존 보상
        if ((highAttackZone != null && highAttackZone.activeSelf) ||
             (lowAttackZone != null && lowAttackZone.activeSelf) ||
             (dashAttackZone != null && dashAttackZone.activeSelf) ||
             (stunZone != null && stunZone.activeSelf) ||
             isAnyUltActive)
        {
            AddReward(0.002f);
        }
    }

    public float GetReactionDelay()
    {
        switch (currentPersona)
        {
            case PersonaType.Beginner: return 0.25f;
            case PersonaType.Intermediate: return 0.125f;
            case PersonaType.Expert: return 0.0f;         // 즉각 반응
            default: return 0.15f;
        }
    }

    void Duck(bool ducking)
    {
        if (isDucking == ducking) return;
        isDucking = ducking;

        if (ducking)
        {
            col.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            col.offset = new Vector2(originalOffset.x, originalOffset.y - (originalSize.y * 0.25f));
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);
            transform.position = new Vector3(transform.position.x, transform.position.y - (originalScale.y * 0.25f), transform.position.z);
        }
        else
        {
            col.size = originalSize;
            col.offset = originalOffset;
            transform.localScale = originalScale;
            transform.position = new Vector3(transform.position.x, transform.position.y + (originalScale.y * 0.25f), transform.position.z);
        }
    }
    System.Collections.IEnumerator JumpCooldownRoutine()
    {
        isJumpCooldown = true;
        yield return new WaitForSeconds(jumpCooldown); // jumpCooldownTime을 jumpCooldown으로 수정
        isJumpCooldown = false;
    }

    System.Collections.IEnumerator EnhancedDashRoutine()
    {
        isDashing = true;
        float originalGravity = rb.gravityScale;

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(facingDir * dashForce, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false; 

        isDashCooldown = true;
        yield return new WaitForSeconds(dashCooldown);
        isDashCooldown = false; // 쿨타임 종료
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            isGrounded = false;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 상단 공격 회피
        if (collision.CompareTag("HighAttack"))
        {
            // 숙이지 않은 상태라면 피격!
            if (!isDucking)
            {
                TakeDamage("상단 공격");
            }
        }
        // 하단 공격 회피
        else if (collision.CompareTag("LowAttack"))
        {
            if (isGrounded)
            {
                TakeDamage("하단 공격");
            }
        }
    }

    void TakeDamage(string attackType)
    {
        FindObjectOfType<QAAnalyzer>()?.OnPlayerHit();

        float penalty = 0f;
        switch (currentPersona)
        {
            case PersonaType.Beginner: penalty = -0.5f; break;      
            case PersonaType.Intermediate: penalty = -1.0f; break;  
            case PersonaType.Expert: penalty = -2.0f; break; 
        }

        AddReward(penalty);
        EndEpisode();
    }

    // ===== ML-AGENTS =====
    public override void OnEpisodeBegin()
    {
        transform.localPosition = startPos;
        rb.velocity = Vector2.zero;
        jumpCount = 0;
        isGrounded = true;
        isDashing = false;
        Duck(false);
        timeInStunZone = 0f;
        rb.gravityScale = 1.25f;

        highTimer = lowTimer = dashTimer = stunTimer = 0f;
        for (int i = 0; i < 5; i++) ultTimers[i] = 0f;
        // (필요하다면 보스 위치와 장판 상태도 여기서 초기화)
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float delay = GetReactionDelay();
        // 내 위치 (2개)
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);

        // 보스 위치 (2개) - 1단계에서는 X축 추격, 나중엔 더 복잡해질 수 있음
        sensor.AddObservation(bossTransform != null ? bossTransform.localPosition.x : 0f);
        sensor.AddObservation(bossTransform != null ? bossTransform.localPosition.y : 0f);

        // 1단계: 상/하단 장판 켜짐 여부 (4개)
        bool isHighActive = highAttackZone != null && highAttackZone.activeSelf && (highTimer >= delay);
        bool isHighAttacking = isHighActive && highAttackZone.GetComponent<Collider2D>().enabled;
        sensor.AddObservation(isHighActive ? 1.0f : 0.0f);      // 예고 단계(0.5초) 포함 켜짐 여부
        sensor.AddObservation(isHighAttacking ? 1.0f : 0.0f);   // 실제 피격 판정(0.25초) 여부

        bool isLowActive = lowAttackZone != null && lowAttackZone.activeSelf && (lowTimer >= delay);
        bool isLowAttacking = isLowActive && lowAttackZone.GetComponent<Collider2D>().enabled;
        sensor.AddObservation(isLowActive ? 1.0f : 0.0f);       // 예고 단계 포함 켜짐 여부
        sensor.AddObservation(isLowAttacking ? 1.0f : 0.0f);    // 실제 피격 판정 여부

        // 보스 딜링(점수) 장판 (상태 1개 + 좌표 2개 = 총 3개) 
        bool isScoringActive = bossScoringZone != null && bossScoringZone.activeSelf;
        sensor.AddObservation(isScoringActive ? 1.0f : 0.0f);
        sensor.AddObservation(bossScoringZone != null ? bossScoringZone.transform.localPosition.x : 0f);
        sensor.AddObservation(bossScoringZone != null ? bossScoringZone.transform.localPosition.y : 0f);

        // 2단계: 대쉬 회피 장판 (상태 2개 + 위치 X, Y 2개 = 총 4개)
        bool isDashActive = dashAttackZone != null && dashAttackZone.activeSelf && (dashTimer >= delay);
        bool isDashAttacking = isDashActive && dashAttackZone.GetComponent<Collider2D>() != null && dashAttackZone.GetComponent<Collider2D>().enabled;
        sensor.AddObservation(isDashActive ? 1.0f : 0.0f);
        sensor.AddObservation(isDashAttacking ? 1.0f : 0.0f);
        sensor.AddObservation(dashAttackZone != null ? dashAttackZone.transform.localPosition.x : 0f);
        sensor.AddObservation(dashAttackZone != null ? dashAttackZone.transform.localPosition.y : 0f);

        // 2단계: 기절 장판 (상태 2개 + 위치 X, Y 2개 = 총 4개)
        bool isStunActive = stunZone != null && stunZone.activeSelf && (stunTimer >= delay);
        bool isStunAttacking = isStunActive && stunZone.GetComponent<Collider2D>() != null && stunZone.GetComponent<Collider2D>().enabled;
        sensor.AddObservation(isStunActive ? 1.0f : 0.0f);
        sensor.AddObservation(isStunAttacking ? 1.0f : 0.0f);
        sensor.AddObservation(stunZone != null ? stunZone.transform.localPosition.x : 0f);
        sensor.AddObservation(stunZone != null ? stunZone.transform.localPosition.y : 0f);

        // 3단계: 궁극기 (상태 1개 + 각 좌표 20개 = 총 21개)
        bool isUltActive = ultimateZones.Length > 0 && ultimateZones[0] != null && ultimateZones[0].activeSelf && (ultTimers[0] >= delay);
        sensor.AddObservation(isUltActive ? 1.0f : 0.0f);
        for (int i = 0; i < 5; i++)
        {
            if (i < ultimateZones.Length && ultimateZones[i] != null && ultimateZones[i].activeSelf && (ultTimers[i] >= delay))
            {
                var ultScript = ultimateZones[i].GetComponent<UltimateLineAttack>();

                // 시작점 (X1, Y1)
                sensor.AddObservation(ultScript.startPoint.x);
                sensor.AddObservation(ultScript.startPoint.y);
                // 끝점 (X2, Y2)
                sensor.AddObservation(ultScript.endPoint.x);
                sensor.AddObservation(ultScript.endPoint.y);
            }
            else
            {
                // 안 켜져 있으면 0으로 채움
                sensor.AddObservation(0f); sensor.AddObservation(0f);
                sensor.AddObservation(0f); sensor.AddObservation(0f);
            }
        }

        // 페르소나에 따른 반응 지연 시간
        sensor.AddObservation(delay);

        // 총 확보된 Observation Size: 41
    }

    // 행동 수신 및 실행
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isDashing) return;

        int moveAction = actions.DiscreteActions[0]; // 0: 정지, 1: 좌, 2: 우
        int jumpAction = actions.DiscreteActions[1]; // 0: 정지, 1: 점프
        int duckAction = actions.DiscreteActions[2]; // 0: 정지, 1: 숙이기
        int dashAction = actions.DiscreteActions[3]; // 0: 정지, 1: 대쉬

        // 1. 이동
        float moveInput = 0f;
        if (moveAction == 1) moveInput = -1f;
        else if (moveAction == 2) moveInput = 1f;

        float currentMoveSpeed = isGrounded ? moveSpeed : moveSpeed * 0.7f;
        rb.velocity = new Vector2(moveInput * currentMoveSpeed, rb.velocity.y);
        if (moveInput != 0) facingDir = moveInput;

        // 2. 점프
        if (jumpAction == 1 && prevJumpAction == 0)
        {

            if (jumpCount < maxJumps && !isJumpCooldown)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                jumpCount++;
                StartCoroutine(JumpCooldownRoutine());
            }
            else if (isJumpCooldown)
            {
                AddReward(-0.1f);
            }
        }

        // 3. 숙이기
        if (duckAction == 1 && isGrounded)
        {
            Duck(true);
        }
        else
        {
            Duck(false);
        }

        // 4. 대쉬
        if (dashAction == 1 && prevDashAction == 0)
        {
            if (!isDashing && !isDashCooldown)
            {
                bool canDash = false;
                if (isGrounded) canDash = true;
                else if (airDashCount < maxAirDashes)
                {
                    canDash = true;
                    airDashCount++;
                }

                if (canDash) StartCoroutine(EnhancedDashRoutine());
            }
            else
            {
                AddReward(-0.1f);
            }
        }


        prevJumpAction = jumpAction;
        prevDashAction = dashAction;
    }

    //  키보드 수동 조작
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;

        // 이동
        if (Input.GetAxisRaw("Horizontal") < 0) discreteActions[0] = 1;
        else if (Input.GetAxisRaw("Horizontal") > 0) discreteActions[0] = 2;
        else discreteActions[0] = 0;

        // 점프
        if (jumpRequested)
        {
            discreteActions[1] = 1;
            jumpRequested = false;
        }
        else
        {
            discreteActions[1] = 0;
        }

        // 숙이기
        if (Input.GetKey(KeyCode.DownArrow)) discreteActions[2] = 1;
        else discreteActions[2] = 0;

        // 대쉬
        if (dashRequested)
        {
            discreteActions[3] = 1;
            dashRequested = false;
        }
        else
        {
            discreteActions[3] = 0;
        }
    }
}
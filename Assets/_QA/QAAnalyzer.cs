using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class QAAnalyzer : MonoBehaviour
{
    [Header("Target References")]
    public PlayerController player;
    public BossPattern_Phase1 boss;

    [Header("UI Display (Optional)")]
    public Text qaResultText;

    private int totalAttacks = 0;
    private int hitCount = 0;
    private float dodgeRate = 0f;

    [Header("Test Settings")]
    public float testDurationSeconds = 3000f; // 기본값 300초 (5분)
    private bool isTestFinished = false;

    [Header("Time Acceleration")]
    [Range(1f, 10f)]
    public float timeMultiplier = 5f;

    void Start()
    {
        if (player == null || boss == null)
        {
            Debug.LogError("QAAnalyzer: 플레이어나 보스 스크립트가 연결되지 않았습니다!");
            enabled = false;
            return;
        }

        ResetStatistics();
        StartCoroutine(AutoTestRoutine());
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        Time.timeScale = timeMultiplier;

        if (Input.GetKeyDown(KeyCode.R))
        {
            StopAllCoroutines(); // 기존 타이머 정지
            ResetStatistics();
            StartCoroutine(AutoTestRoutine()); // 타이머 재시작
            Debug.Log("QA 통계가 초기화되었습니다.");
        }
    }

    IEnumerator AutoTestRoutine()
    {
        Debug.Log($"<color=cyan>[QA] 자동 측정을 시작합니다. {testDurationSeconds}초 뒤 종료됩니다.</color>");
        yield return new WaitForSeconds(testDurationSeconds);

        isTestFinished = true;
        PrintFinalReport();
        Debug.Break();
    }

    public void ResetStatistics()
    {
        totalAttacks = 0;
        hitCount = 0;
        dodgeRate = 0f;
        isTestFinished = false;
        UpdateUIDisplay();
    }

    public void OnAttackStarted()
    {
        if (isTestFinished) return;
        totalAttacks++;
        CalculateDodgeRate();
        UpdateUIDisplay();
    }

    public void OnPlayerHit()
    {
        if (isTestFinished) return;
        hitCount++;
        CalculateDodgeRate();
        UpdateUIDisplay();
    }

    private void CalculateDodgeRate()
    {
        if (totalAttacks <= 0)
        {
            dodgeRate = 0f;
            return;
        }
        int dodgeCount = totalAttacks - hitCount;
        dodgeRate = (float)dodgeCount / totalAttacks * 100f;
    }

    // 화면 UI에는 실시간 변동만 간략하게 표시
    private void UpdateUIDisplay()
    {
        string personaName = player.currentPersona.ToString();
        string report =
            $"[실시간 QA - {personaName}]\n" +
            $"공격: {totalAttacks} | 피격: {hitCount}\n" +
            $"회피율: {dodgeRate:F1}%";

        if (qaResultText != null)
        {
            qaResultText.text = report;
        }
    }

    private void PrintFinalReport()
    {
        string personaName = player.currentPersona.ToString();
        int dodgeCount = totalAttacks - hitCount;

        string finalReport =
            $"<b><size=14><color=yellow>[최종 QA 리포트 - {personaName}]</color></size></b>\n" +
            $"측정 시간 : {testDurationSeconds}초\n" +
            $"-----------------------------\n" +
            $"총 공격 횟수 (Attempts) : <b>{totalAttacks}</b>\n" +
            $"피격 횟수 (Hits)      : <b>{hitCount}</b>\n" +
            $"회피 횟수 (Dodges)    : <b>{dodgeCount}</b>\n" +
            $"-----------------------------\n" +
            $"최종 회피율 (Dodge Rate) : <b><color=green>{dodgeRate:F1}%</color></b>";

        Debug.LogWarning(finalReport);
    }
}
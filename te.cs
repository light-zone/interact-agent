using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection; // 리플렉션을 위해 추가
/*
/// <summary>
/// 에이전트의 실시간 학습 메커니즘을 테스트하고 검증하기 위한 자동화된 테스트 스크립트입니다.
/// 실제 프로젝트 코드 분석을 바탕으로 작성되었습니다.
///
/// [사용 방법]
/// 1. 씬에 비어있는 새 게임 오브젝트를 생성하고 "AgentTestRunner"라고 이름 짓습니다.
/// 2. 이 'te.cs' 스크립트를 "AgentTestRunner" 오브젝트에 추가(컴포넌트로 부착)합니다.
/// 3. 인스펙터 창에 나타난 'Agent To Test' 필드에 씬에 있는 에이전트(NewBehaviourScript를 가진) 오브젝트를 끌어다 놓습니다.
/// 4. 게임을 플레이하면, 콘솔 창에 테스트 시나리오 진행 과정과 에이전트의 학습 상태가 자동으로 출력됩니다.
/// </summary>
public class te : MonoBehaviour
{
    [Tooltip("테스트할 에이전트 오브젝트(NewBehaviourScript를 가진)를 여기에 할당하세요.")]
    public NewBehaviourScript agentToTest;

    [Tooltip("테스트를 위해 에이전트의 private 필드인 'learnedValues'에 접근할지 여부입니다.")]
    public bool accessPrivateFields = true;

    // 리플렉션을 통해 가져온 private 필드 정보를 저장할 변수
    private FieldInfo learnedValuesField;

    void Start()
    {
        if (agentToTest == null)
        {
            Debug.LogError("테스트할 에이전트가 할당되지 않았습니다! 스크립트의 'Agent To Test' 필드에 에이전트를 할당해주세요.");
            return;
        }

        if (accessPrivateFields)
        {
            // 리플렉션을 사용하여 NewBehaviourScript의 private 필드 'learnedValues'에 접근 준비
            learnedValuesField = typeof(NewBehaviourScript).GetField("learnedValues", BindingFlags.NonPublic | BindingFlags.Instance);
            if (learnedValuesField == null)
            {
                Debug.LogError("'learnedValues' 필드를 찾을 수 없습니다. 이름이 변경되었거나 코드가 바뀌었을 수 있습니다.");
                accessPrivateFields = false;
            }
        }

        StartCoroutine(RunAllTestScenarios());
    }

    private IEnumerator RunAllTestScenarios()
    {
        Debug.LogWarning("========= 에이전트 학습 메커니즘 테스트 시작 =========");
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(Scenario_NegativeReinforcement());
        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(Scenario_PositiveReinforcement());
        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(Scenario_LearningReversal());
        yield return new WaitForSeconds(2f);

        Debug.LogWarning("========= 모든 테스트 시나리오 완료 =========");
    }

    private IEnumerator Scenario_NegativeReinforcement()
    {
        Debug.Log("--- [시나리오 1 시작] 부정적 강화: '체력이 높고 플레이어가 가까울 때' 반복 공격 ---");
        LogAgentBrainState("테스트 시작 전");

        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"[시나리오 1] {i + 1}/3회차 공격 시뮬레이션.");
            agentToTest.TakeDamage(10);
            yield return new WaitForSeconds(1.0f); // toutch.cs의 nag()가 발동하도록 짧은 간격으로 호출
        }
        
        LogAgentBrainState("공격 종료 후");
        Debug.Log("--- [시나리오 1 종료] 'Health:High', 'Player:Near' 등의 가치가 음수로 학습되었는지 확인하세요. ---");
    }

    private IEnumerator Scenario_PositiveReinforcement()
    {
        Debug.Log("--- [시나리오 2 시작] 긍정적 강화: 반복적으로 음식 제공 ---");
        
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"[시나리오 2] {i + 1}/3회차 음식 제공 시뮬레이션.");
            agentToTest.GiveFood(10);
            yield return new WaitForSeconds(2.0f); // nag()가 발동하지 않도록 긴 간격으로 호출
        }

        LogAgentBrainState("음식 제공 종료 후");
        Debug.Log("--- [시나리오 2 종료] 관련 상태 태그의 가치가 양수로 학습되었는지 확인하세요. ---");
    }

    private IEnumerator Scenario_LearningReversal()
    {
        Debug.Log("--- [시나리오 3 시작] 학습 역전: 부정적 학습 후 긍정적 학습 진행 ---");
        Debug.LogWarning("'Health:High', 'Player:Near' 태그가 음수인 상태에서 시작합니다.");

        for (int i = 0; i < 5; i++)
        {
            Debug.Log($"[시나리오 3] {i + 1}/5회차 '접근 시 보상' 시뮬레이션.");
            agentToTest.GiveFood(10);
            yield return new WaitForSeconds(2.0f);
        }

        LogAgentBrainState("학습 역전 시도 후");
        Debug.Log("--- [시나리오 3 종료] 부정적으로 학습되었던 태그의 가치가 다시 양수 방향으로 회복되었는지 확인하세요. ---");
    }

    /// <summary>
    /// 에이전트의 현재 학습 상태 (가중치, 편향 등)를 콘솔에 출력합니다.
    /// </summary>
    private void LogAgentBrainState(string context)
    {
        // 1. 공개된 정보 로깅
        Debug.Log($"[AGENT STATE][{context}] 관계점수: {agentToTest.relationshipScore:F2}");

        // 2. 리플렉션을 통해 private 'learnedValues' 필드 값 로깅
        if (accessPrivateFields && learnedValuesField != null)
        {
            var learnedValues = learnedValuesField.GetValue(agentToTest) as Dictionary<string, float>;
            if (learnedValues != null && learnedValues.Count > 0)
            {
                string valuesLog = "";
                foreach (var pair in learnedValues)
                {
                    valuesLog += $"[{pair.Key}: {pair.Value:F2}] ";
                }
                Debug.Log($"[BRAIN STATE][{context}] 학습된 가치(learnedValues): {valuesLog}");
            }
            else
            {
                Debug.Log($"[BRAIN STATE][{context}] 'learnedValues'가 비어있습니다.");
            }
        }
        else
        {
            Debug.LogWarning("=> 'learnedValues'는 private 필드입니다. 정확한 값 확인을 위해 'accessPrivateFields'를 체크하거나, 해당 필드를 public으로 변경하는 것을 권장합니다.");
        }
    }
}
*/
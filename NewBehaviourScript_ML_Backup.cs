/*using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class NewBehaviourScript : Agent
{
    // --- 변수들 ---
    float[] cot = new float[] { 0, 0, 0 };
    float currentHealth = 100f;
    float currentSanity = 100f;
    public Transform player;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float attitudeInfluence = 1f;

    [Header("Relationship Settings")]
    private float relationshipScore = 0f;
    public float relationshipDecayRate = 0.5f;
    float lastDistance;
    private float timeOfLastInteraction = -100f;
    
    // 최종적으로 움직임에 사용될 에이전트의 '태도'
    private float playerAttitude = 0f;

    public override void OnEpisodeBegin()
    {
        currentHealth = 100f;
        currentSanity = 100f;
        relationshipScore = 0f;
        if (player != null) { 
            lastDistance = Vector2.Distance(transform.position, player.position);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(currentHealth / 100f);
        sensor.AddObservation(currentSanity / 100f);
        sensor.AddObservation(relationshipScore / 100f);
        if (player != null)
        {
            float currentDistance = Vector2.Distance(transform.position, player.position);
            sensor.AddObservation(currentDistance);
            sensor.AddObservation(lastDistance - currentDistance);
            lastDistance = currentDistance;
        }
        else
        { 
            sensor.AddObservation(0f); sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // --- 새로운 태도 결정 로직 ---
        // 1. 뇌가 현재 상황에 따라 판단한 '순간적인 기분 변화량'(-1 ~ +1)을 가져옵니다.
        float learnedMoodModifier = actionBuffers.ContinuousActions[0];

        // 2. 장기적인 관계 점수를 기본 기분으로 설정합니다. (-1 ~ +1로 정규화)
        float baseMood = relationshipScore / 100f;

        // 3. 최종 태도 = (기본 기분) + (순간적인 기분 변화량) 으로 결정합니다.
        playerAttitude = baseMood + learnedMoodModifier;
        playerAttitude = Mathf.Clamp(playerAttitude, -1f, 1f); // 최종 값을 -1 ~ +1 사이로 고정

        // 디버깅을 위해 현재 태도 값을 로그로 출력
        Debug.Log($"관계점수: {relationshipScore:F1}, 학습된변화량: {learnedMoodModifier:F1}, 최종태도: {playerAttitude:F1}");

        SetReward(-0.001f);
    }

    void Update()
    {
        if (player != null)
        {
            Vector3 moveDirection = (player.position - transform.position).normalized * playerAttitude;
            transform.position += moveDirection * moveSpeed * attitudeInfluence * Time.deltaTime;
        }
        relationshipScore = Mathf.MoveTowards(relationshipScore, 0, relationshipDecayRate * Time.deltaTime);
    }

    // --- 상호작용 함수들 ---
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        AddReward(-0.1f);

        float interval = Time.time - timeOfLastInteraction;
        float frequencyMultiplier = 1.0f / Mathf.Max(interval, 0.2f);
        relationshipScore -= damage * frequencyMultiplier;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }

    public void GiveFood(float amount)
    {
        currentHealth += amount;
        AddReward(0.2f);
        relationshipScore += amount;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }

    public void ReactToTrick(float amount)
    {
        currentSanity += amount;
        AddReward(0.5f);
        relationshipScore += amount * 2.0f;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }
}
*/
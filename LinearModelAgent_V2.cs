
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LinearModelAgent_V2 : MonoBehaviour
{
    [Header("Agent Stats")]
    public float hp = 100f;
    public float moveSpeed = 3f;

    [Header("Target")]
    public Transform player;

    [Header("Learning Rate Settings")]
    [SerializeField] private float initial_learning_rate = 0.01f;
    [SerializeField] private float min_learning_rate = 0.001f;
    [SerializeField] private float learning_rate_decay = 0.02f;
    [SerializeField] private float base_shock_error_threshold = 1.0f;
    private int interaction_count = 0;

    [Header("Meta-Learning (Confidence)")]
    [SerializeField] private int predictionSuccessStreak = 0;
    [SerializeField] private float confidence_scale = 0.1f;

    [Header("Context Learning")]
    [SerializeField] private float context_learning_rate = 0.1f;
    private Dictionary<string, float> learnedValues = new Dictionary<string, float>();

    [Header("Learning Parameters")]
    [SerializeField] [Range(0, 0.1f)] private float explorationChance = 0.05f;

    // === 신경망 구조 ===
    private const int NUM_INPUTS = 2;
    private const int NUM_HIDDEN = 3;
    private float[,] weights_input_hidden;
    private float[] biases_hidden;
    private float[] weights_hidden_output;
    private float bias_output;

    private float[] last_inputs;
    private float[] last_hidden_sums;
    private float[] last_hidden_outputs;
    private float last_mlp_score;
    private float previousDistanceToPlayer;

    public enum AgentAction { Idle, ApproachingPlayer, Fleeing }
    private AgentAction currentAction;

    private SpriteRenderer agentRenderer;

    #region MonoBehaviour Lifecycle
    void Start()
    {
        if (player == null) { enabled = false; return; }
        agentRenderer = GetComponent<SpriteRenderer>();
        InitializeNeuralNetwork();
        previousDistanceToPlayer = Vector2.Distance(transform.position, player.position);
        DecideNextAction();
    }

    void Update()
    {
        ExecuteAction();
        UpdateColor();
        if (Time.frameCount % 120 == 0) DecideNextAction();
    }
    #endregion

    #region Neural Network Core
    void InitializeNeuralNetwork() { /* ... */ }
    float ForwardPass(float[] inputs) { /* ... */ }
    void BackwardPass(float error_signal) { /* ... */ }
    private float ReLU(float x) => Mathf.Max(0, x);
    private float ReLU_Derivative(float x) => x > 0 ? 1 : 0;
    #endregion

    #region Agent Logic & Learning
    float GetCurrentLearningRate()
    {
        float current_lr = initial_learning_rate / (1.0f + learning_rate_decay * interaction_count);
        return Mathf.Max(current_lr, min_learning_rate);
    }

    void HandleInteraction(float target_score, float hp_change)
    {
        interaction_count++;
        float error = target_score - last_mlp_score;

        // ★ 메타 학습 2: '자신감' 시스템
        float current_shock_threshold = base_shock_error_threshold + (predictionSuccessStreak * confidence_scale);
        if (Mathf.Abs(error) > current_shock_threshold)
        {
            predictionSuccessStreak = 0; // 예측이 크게 틀렸으므로 자신감 초기화
            interaction_count = 0; // 학습 카운터도 초기화하여 학습률을 높임
            Debug.LogWarning("충격적 사건 발생! 자신감 및 학습 카운터 초기화.");
        }
        else
        {
            predictionSuccessStreak++; // 예측이 맞았으므로 자신감 증가
        }

        BackwardPass(error);
        LearnFromContext(hp_change != 0 ? (hp_change < 0) : (bool?)null);
        DecideNextAction();
    }

    void DecideNextAction()
    {
        if (Random.value < explorationChance) { /* ... */ }

        last_inputs = new float[NUM_INPUTS];
        float currentDistance = Vector2.Distance(transform.position, player.position);
        float distanceChange = currentDistance - previousDistanceToPlayer;
        last_inputs[0] = Mathf.Clamp01(currentDistance / 20f);
        last_inputs[1] = Mathf.Clamp(distanceChange, -1f, 1f);

        last_mlp_score = ForwardPass(last_inputs);

        // 행동 결정은 순수 MLP 점수만 사용
        if (last_mlp_score > 0.1f) currentAction = AgentAction.ApproachingPlayer;
        else if (last_mlp_score < -0.1f) currentAction = AgentAction.Fleeing;
        else currentAction = AgentAction.Idle;

        previousDistanceToPlayer = currentDistance;
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        float hp_change = -damage;
        float target_score = -0.5f + (hp_change / 100f) * 0.5f;
        HandleInteraction(target_score, hp_change);
    }

    public void ReceivePet(float healAmount)
    {
        hp += healAmount;
        hp = Mathf.Clamp(hp, 0, 100f);
        float target_score = 0.8f;
        HandleInteraction(target_score, healAmount);
    }

    // ★ 메타 학습 1: 독립적인 맥락 학습
    void LearnFromContext(bool? was_outcome_negative)
    {
        if (was_outcome_negative == null) return; // 아무일도 없었으면 학습 안함

        List<string> tags = GetCurrentStateTags();
        foreach (var tag in tags)
        {
            if (!learnedValues.ContainsKey(tag)) learnedValues[tag] = 0;

            float context_predicts_negative = learnedValues[tag] < 0 ? 1f : 0f;
            float outcome_is_negative = was_outcome_negative.Value ? 1f : 0f;

            // 예측과 결과가 일치하면 강화, 틀리면 약화
            float context_error = outcome_is_negative - context_predicts_negative;
            
            // 자신감이 높을수록 맥락은 덜 변함
            float context_lr = context_learning_rate / (1f + predictionSuccessStreak * confidence_scale);

            learnedValues[tag] += context_lr * context_error;
            learnedValues[tag] = Mathf.Clamp(learnedValues[tag], -50f, 50f);
        }
    }

    List<string> GetCurrentStateTags()
    {
        List<string> tags = new List<string>();
        float currentDistance = Vector2.Distance(transform.position, player.position);
        if (currentDistance < 5f) tags.Add("Distance:Near");
        else if (currentDistance < 15f) tags.Add("Distance:Mid");
        else tags.Add("Distance:Far");
        return tags;
    }
    #endregion

    #region Boilerplate
    void ExecuteAction() { /* ... */ }
    void UpdateColor() { /* ... */ }
    private void OnCollisionEnter2D(Collision2D collision) { /* ... */ }
    #endregion
}

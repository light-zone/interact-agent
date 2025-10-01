using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LinearModelAgent : MonoBehaviour
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
    [SerializeField] private float shock_error_threshold = 1.0f;
    private int interaction_count = 0;

    [Header("Context Learning")]
    [SerializeField] private float contextBiasScale = 0.05f;
    private Dictionary<string, float> learnedValues = new Dictionary<string, float>();

    [Header("Reward-Seeking (Dopamine)")]
    [SerializeField] private float expectedRewardLevel = 0f; // 기대 보상 수준 (갈망의 척도)
    [SerializeField] private float rewardMemoryDecay = 0.1f;   // 기대 보상 기억의 감쇠율
    [SerializeField] private float rewardOnPet = 5.0f;         // 쓰다듬기 시 얻는 보상량
    [SerializeField] private float rewardDeficitBiasScale = 0.1f; // 보상 결핍이 판단에 미치는 영향력

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
        // 기대 보상 수준(과거의 즐거운 기억)은 시간에 따라 서서히 감소
        expectedRewardLevel -= rewardMemoryDecay * Time.deltaTime;
        expectedRewardLevel = Mathf.Max(0, expectedRewardLevel);

        ExecuteAction();
        UpdateColor();
        if (Time.frameCount % 120 == 0) DecideNextAction();
    }
    #endregion

    #region Neural Network Core
    void InitializeNeuralNetwork()
    {
        weights_input_hidden = new float[NUM_INPUTS, NUM_HIDDEN];
        biases_hidden = new float[NUM_HIDDEN];
        weights_hidden_output = new float[NUM_HIDDEN];
        for (int i = 0; i < NUM_INPUTS; i++) for (int j = 0; j < NUM_HIDDEN; j++) weights_input_hidden[i, j] = Random.Range(-0.1f, 0.1f);
        for (int i = 0; i < NUM_HIDDEN; i++) biases_hidden[i] = Random.Range(-0.1f, 0.1f);
        for (int i = 0; i < NUM_HIDDEN; i++) weights_hidden_output[i] = Random.Range(-0.1f, 0.1f);
        bias_output = Random.Range(-0.1f, 0.1f);
    }

    float ForwardPass(float[] inputs)
    {
        last_hidden_sums = new float[NUM_HIDDEN];
        last_hidden_outputs = new float[NUM_HIDDEN];
        for (int j = 0; j < NUM_HIDDEN; j++)
        {
            float sum = 0;
            for (int i = 0; i < NUM_INPUTS; i++) sum += inputs[i] * weights_input_hidden[i, j];
            sum += biases_hidden[j];
            last_hidden_sums[j] = sum;
            last_hidden_outputs[j] = ReLU(sum);
        }
        float output_sum = 0;
        for (int i = 0; i < NUM_HIDDEN; i++) output_sum += last_hidden_outputs[i] * weights_hidden_output[i];
        output_sum += bias_output;
        return output_sum;
    }

    void BackwardPass(float error_signal)
    {
        float current_lr = GetCurrentLearningRate();
        for (int i = 0; i < NUM_HIDDEN; i++) weights_hidden_output[i] += current_lr * error_signal * last_hidden_outputs[i];
        bias_output += current_lr * error_signal;
        float[] hidden_errors = new float[NUM_HIDDEN];
        for (int i = 0; i < NUM_HIDDEN; i++) hidden_errors[i] = error_signal * weights_hidden_output[i];
        for (int j = 0; j < NUM_HIDDEN; j++)
        {
            float delta_hidden = hidden_errors[j] * ReLU_Derivative(last_hidden_sums[j]);
            for (int i = 0; i < NUM_INPUTS; i++) weights_input_hidden[i, j] += current_lr * delta_hidden * last_inputs[i];
            biases_hidden[j] += current_lr * delta_hidden;
        }
    }

    private float ReLU(float x) => Mathf.Max(0, x);
    private float ReLU_Derivative(float x) => x > 0 ? 1 : 0;
    #endregion

    #region Agent Logic & Learning
    float GetCurrentLearningRate()
    {
        float current_lr = initial_learning_rate / (1.0f + learning_rate_decay * interaction_count);
        return Mathf.Max(current_lr, min_learning_rate);
    }

    void HandleInteraction(float target_score)
    {
        interaction_count++;
        float error = target_score - last_mlp_score;

        if (Mathf.Abs(error) > shock_error_threshold)
        {
            int previous_count = interaction_count;
            interaction_count = 0;
            Debug.LogWarning("충격적 사건 발생! 학습 카운터 초기화: " + previous_count + " -> " + interaction_count);
        }

        BackwardPass(error);
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

        // 1. 신경망의 순수 상황 판단
        float mlp_score = ForwardPass(last_inputs);
        last_mlp_score = mlp_score; // 학습을 위해 순수 점수 저장

        // 2. 맥락적 편향 계산
        List<string> currentTags = GetCurrentStateTags();
        float contextual_bias = 0f;
        foreach (var tag in currentTags) if (learnedValues.ContainsKey(tag)) contextual_bias += learnedValues[tag];

        // 3. 보상 추구 편향 계산
        float reward_deficit_bias = expectedRewardLevel * rewardDeficitBiasScale;

        // 4. 최종 점수 = 상황판단 + 맥락편향 + 보상추구편향
        float final_score = mlp_score + (contextual_bias * contextBiasScale) + reward_deficit_bias;

        if (final_score > 0.1f) currentAction = AgentAction.ApproachingPlayer;
        else if (final_score < -0.1f) currentAction = AgentAction.Fleeing;
        else currentAction = AgentAction.Idle;

        previousDistanceToPlayer = currentDistance;
    }

    public void TakeDamage(float damage)
    {
        expectedRewardLevel = 0f; // 피격 시, 기대 보상(즐거웠던 기억) 즉시 초기화
        hp -= damage;
        float hp_change = -damage;
        float target_score = -0.5f + (hp_change / 100f) * 0.5f;
        HandleInteraction(target_score);
    }

    public void ReceivePet(float healAmount)
    {
        expectedRewardLevel += rewardOnPet; // 쓰다듬어주면 기대 보상 수준 증가
        hp += healAmount;
        hp = Mathf.Clamp(hp, 0, 100f);
        float target_score = 0.8f;
        HandleInteraction(target_score);
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
    void ExecuteAction()
    {
        Vector2 baseDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        switch (currentAction)
        {
            case AgentAction.ApproachingPlayer: transform.position += (Vector3)baseDirection * moveSpeed * Time.deltaTime; break;
            case AgentAction.Fleeing: transform.position -= (Vector3)baseDirection * moveSpeed * Time.deltaTime; break;
            case AgentAction.Idle: break;
        }
    }

    void UpdateColor()
    {
        float display_score = Mathf.Clamp(last_mlp_score, -1f, 1f);
        if (display_score > 0) agentRenderer.color = Color.Lerp(Color.white, Color.green, display_score);
        else agentRenderer.color = Color.Lerp(Color.white, Color.red, -display_score);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        MouseFollower playerScript = collision.gameObject.GetComponent<MouseFollower>();
        if (playerScript != null)
        {
            if (playerScript.n == 1)
            {
                TakeDamage(10);
            }
            else
            {
                ReceivePet(5);
            }
        }
    }
    #endregion
}


using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private float initial_learning_rate = 0.03f;
    [SerializeField] private float min_learning_rate = 0.001f;
    [SerializeField] private float learning_rate_decay = 0.02f;
    [SerializeField] private float shock_error_threshold = 0.5f; // 이 오차 이상일 때 '충격'으로 간주
    private int interaction_count = 0;

    [Header("Context Learning")]
    [SerializeField] private float contextBiasScale = 0.05f;
    private Dictionary<string, float> learnedValues = new Dictionary<string, float>();

    [Header("Learning Parameters")]
    [SerializeField] [Range(0, 0.1f)] private float explorationChance = 0.02f;

    // === 신경망 구조 ===
    private const int NUM_INPUTS = 2; // 1:거리, 2:접근속도
    private const int NUM_HIDDEN = 3;
    private float[,] weights_input_hidden;
    private float[] biases_hidden;
    private float[] weights_hidden_output;
    private float bias_output;

    // === 학습 및 상태 추적용 내부 변수 ===
    private float[] last_inputs;
    private float[] last_hidden_sums;
    private float[] last_hidden_outputs;
    private float last_mlp_score; // 신경망의 순수한 예측값
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

    // 순전파 함수. 이제 맥락 편향(contextual_bias)을 직접 받음
    float ForwardPass(float[] inputs, float contextual_bias)
    {
        last_hidden_sums = new float[NUM_HIDDEN];
        last_hidden_outputs = new float[NUM_HIDDEN];
        for (int j = 0; j < NUM_HIDDEN; j++)
        {
            float sum = 0;
            for (int i = 0; i < NUM_INPUTS; i++) sum += inputs[i] * weights_input_hidden[i, j];
            // ★ 핵심 1: 맥락적 편향을 은닉층 계산에 직접 주입
            sum += biases_hidden[j] + contextual_bias;
            last_hidden_sums[j] = sum;
            last_hidden_outputs[j] = ReLU(sum);
        }
        float output_sum = 0;
        for (int i = 0; i < NUM_HIDDEN; i++) output_sum += last_hidden_outputs[i] * weights_hidden_output[i];
        // ★ 핵심 1: 맥락적 편향을 출력층 계산에도 직접 주입
        output_sum += bias_output + contextual_bias;
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

    // 학습을 처리하는 통합 함수
    void HandleInteraction(float target_score)
    {
        interaction_count++;

        // ★ 핵심 2: '충격' 시스템
        // '실제 결과'와 '순수한 결과 예측(last_mlp_score)'의 차이를 통해 '놀람(오차)'의 정도를 계산
        float error = target_score - last_mlp_score;

        // 만약 예측이 너무 크게 빗나가면 (놀람이 크면), '초심'으로 돌아가 학습률을 높임
        if (Mathf.Abs(error) > shock_error_threshold)
        {
            int previous_count = interaction_count;
            interaction_count = 0; // 충격 시, 학습 카운터를 0으로 초기화
            Debug.LogWarning("충격적 사건 발생! 학습 카운터 초기화: " + previous_count + " -> " + interaction_count);
        }

        // 신경망은 이 '오차'를 이용해 역전파 학습
        BackwardPass(error);
        // 맥락 시스템도 동일한 '오차'를 이용해 학습 (유기적 연동)
        LearnFromContext(error);

        DecideNextAction();
    }

    void DecideNextAction()
    {
        if (Random.value < explorationChance) { /* ... */ }

        // 1. 입력 데이터 준비
        last_inputs = new float[NUM_INPUTS];
        float currentDistance = Vector2.Distance(transform.position, player.position);
        float distanceChange = currentDistance - previousDistanceToPlayer;
        last_inputs[0] = Mathf.Clamp01(currentDistance / 20f);
        last_inputs[1] = Mathf.Clamp(distanceChange, -1f, 1f);

        // 2. 맥락 편향 계산
        List<string> currentTags = GetCurrentStateTags();
        float contextual_bias = 0f;
        foreach (var tag in currentTags) if (learnedValues.ContainsKey(tag)) contextual_bias += learnedValues[tag];
        contextual_bias *= contextBiasScale;

        // 3. 신경망을 통해 '행동 판단값' 계산
        //    ForwardPass에 맥락 편향을 직접 주입하여 계산에 영향을 줌
        float action_score = ForwardPass(last_inputs, contextual_bias);

        // 4. '결과 예측값'은 맥락 편향이 없는 순수 신경망의 출력으로 따로 계산하여 저장
        //    이는 다음 '충격' 계산에 사용됨
        last_mlp_score = ForwardPass(last_inputs, 0);

        // 5. 최종 행동 결정 (행동 판단값을 기준으로)
        if (action_score > 0.1f) currentAction = AgentAction.ApproachingPlayer;
        else if (action_score < -0.1f) currentAction = AgentAction.Fleeing;
        else currentAction = AgentAction.Idle;

        previousDistanceToPlayer = currentDistance;
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        float hp_change = -damage;
        float target_score = -0.5f + (hp_change / 100f) * 0.5f;
        HandleInteraction(target_score);
    }

    public void ReceivePet(float healAmount)
    {
        hp += healAmount;
        hp = Mathf.Clamp(hp, 0, 100f);
        float target_score = 0.8f;
        HandleInteraction(target_score);
    }

    // 신경망의 '오차'를 받아 맥락을 학습하는 함수
    void LearnFromContext(float error)
    {
        List<string> tags = GetCurrentStateTags();
        foreach (var tag in tags)
        {
            if (!learnedValues.ContainsKey(tag)) learnedValues[tag] = 0;
            // ★ 핵심 3: 유기적 연동
            // 고정된 값이 아닌, 신경망이 느낀 '오차'에 비례하여 맥락 가중치를 업데이트
            learnedValues[tag] += error * GetCurrentLearningRate() * 20f; // 학습률 및 영향력 조절
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

    #region Boilerplate & Execution
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
        // 색상은 맥락이 포함된 최종 행동 점수가 아닌, 신경망의 순수한 예측을 반영
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

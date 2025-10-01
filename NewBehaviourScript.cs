using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class NewBehaviourScript : MonoBehaviour
{
    // --- 컴포넌트 변수들 ---
    toutch t;
    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;

    // --- 상태 및 체력 변수들 ---
    float currentHealth = 100f;
    float currentSanity = 100f;
    public Transform player;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;

    [Header("AI Settings")]
    public float perceptionRange = 15f;
    public float stoppingDistance = 0.5f;
    public float minDistance = 1f;
    public float maxDistance = 10f;

    [Header("Relationship Settings")]
    private float relationshipScore = 0f;
    public float relationshipDecayRate = 0.5f;
    private float timeOfLastInteraction = -100f;
    private float playerAttitude = 0f;

    [Header("Value System")]
    public float valueInfluence = 0.5f; // 학습된 가치가 태도에 미치는 영향력
    Dictionary<string, float> learnedValues = new Dictionary<string, float>();

    [Header("Visuals")]
    public Color neutralColor = Color.white;
    public Color positiveColor = Color.green;
    public Color negativeColor = new Color(1f, 0.5f, 0f); // Orange
    public Color fleeColor = Color.red;

    [Header("Experience Logging")]
    [SerializeField]
    private List<string> experienceLogs = new List<string>();
    public int maxLogCount = 50;

    void Start()
    {
        t = gameObject.GetComponent<toutch>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        currentHealth = 100f;
        currentSanity = 100f;
        relationshipScore = 0f;
    }

    void Update()
    {
        playerAttitude = relationshipScore / 100f;
        playerAttitude = Mathf.Clamp(playerAttitude, -1f, 1f);
        Debug.Log($"관계점수: {relationshipScore:F1}, 최종태도: {playerAttitude:F1}");
        relationshipScore = Mathf.MoveTowards(relationshipScore, 0, relationshipDecayRate * Time.deltaTime);
        UpdateColor();
    }

    void FixedUpdate()
    {
        if (player == null || rb == null) { if (rb != null) rb.velocity = Vector2.zero; return; }

        float currentDistance = Vector2.Distance(transform.position, player.position);
        if (currentDistance > perceptionRange) { rb.velocity = Vector2.zero; return; }

        if (t.negative == true)
        {
            Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
            rb.velocity = fleeDirection * moveSpeed;
            return;
        }

        float situationalValue = 0f;
        foreach (var tag in GetCurrentStateTags())
        {
            if (learnedValues.ContainsKey(tag))
            {
                situationalValue += learnedValues[tag];
            }
        }
        //예측
        float effectiveAttitude = playerAttitude + (situationalValue * valueInfluence);
        effectiveAttitude = Mathf.Clamp(effectiveAttitude, -1f, 1f);

        float targetDistance = Mathf.Lerp(maxDistance, minDistance, (effectiveAttitude + 1f) / 2f);
        float distanceError = currentDistance - targetDistance;

        if (Mathf.Abs(distanceError) > stoppingDistance)
        {
            Vector2 moveDirection = (distanceError > 0) ? ((Vector2)player.position - (Vector2)transform.position).normalized : ((Vector2)transform.position - (Vector2)player.position).normalized;
            rb.velocity = moveDirection * moveSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    // --- 시각화 시스템 ---
    void UpdateColor()
    {
        if (spriteRenderer == null) return;

        if (t.negative == true)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, fleeColor, Time.deltaTime * 10f);
            return;
        }

        Color targetColor;
        float attitudeNormalized = (playerAttitude + 1f) / 2f;

        if (attitudeNormalized < 0.5f)
        {
            float t = attitudeNormalized * 2f;
            targetColor = Color.Lerp(negativeColor, neutralColor, t);
        }
        else
        {
            float t = (attitudeNormalized - 0.5f) * 2f;
            targetColor = Color.Lerp(neutralColor, positiveColor, t);
        }
        
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 5f);
    }

    // --- 경험 기록 시스템 ---
    private List<string> GetCurrentStateTags()
    {
        List<string> tags = new List<string>();
        if (currentHealth > 70) tags.Add("Health:High");
        else if (currentHealth > 30) tags.Add("Health:Mid");
        else tags.Add("Health:Low");

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < minDistance) tags.Add("Player:TooClose");
        else if (dist < perceptionRange / 2) tags.Add("Player:Near");
        else tags.Add("Player:Far");

        if (playerAttitude > 0.5f) tags.Add("Attitude:Positive");
        else if (playerAttitude < -0.5f) tags.Add("Attitude:Negative");
        else tags.Add("Attitude:Neutral");
        //이동 그거나 행동양식 등도 추가 필요

        return tags;
    }

    private void LogExperience(string action, string outcome, float valueChange)
    {
        List<string> stateTags = GetCurrentStateTags();
        string stateString = string.Join(", ", stateTags);
        string log = $"[State]: {stateString} | [Action]: {action} | [Outcome]: {outcome}";
        experienceLogs.Add(log);
        if (experienceLogs.Count > maxLogCount) { experienceLogs.RemoveAt(0); }
        Debug.Log("New Log: " + log);

        AnalyzeExperience(stateTags, valueChange);
    }

    private void AnalyzeExperience(List<string> tags, float valueChange)
    {
        foreach (var tag in tags)
        {
            if (!learnedValues.ContainsKey(tag))
            {
                learnedValues[tag] = 0;
            }
            learnedValues[tag] += valueChange;
            Debug.Log($"Value Updated: '{tag}' new value = {learnedValues[tag]}");
        }
    }

    // --- 상호작용 함수들 (가치 변화량 전달) ---
    public void TakeDamage(float damage)
    {
        if (t.del == true) { return; }

        float healthBefore = currentHealth;
        float healthChange = -damage;
        LogExperience("TakeDamage", $"HP: {healthBefore:F1} -> {currentHealth + healthChange:F1}", healthChange);

        t.inter();
        currentHealth += healthChange;
        float interval = Time.time - timeOfLastInteraction;
        t.nag(interval);
        float frequencyMultiplier = 1.0f / Mathf.Max(interval, 0.2f);
        relationshipScore -= damage * frequencyMultiplier;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }

    public void GiveFood(float amount)
    {
        if (t.del == true) { return; }

        float healthBefore = currentHealth;
        float healthChange = amount;
        LogExperience("GiveFood", $"HP: {healthBefore:F1} -> {currentHealth + healthChange:F1}", healthChange);

        t.inter();
        currentHealth += healthChange;
        relationshipScore += amount;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }

    public void ReactToTrick(float amount)
    {
        if (t.del == true) { return; }

        float sanityBefore = currentSanity;
        LogExperience("ReactToTrick", $"Sanity: {sanityBefore:F1} -> {currentSanity + amount:F1}", 0);

        t.inter();
        currentSanity += amount;
        relationshipScore += amount * 2.0f;
        relationshipScore = Mathf.Clamp(relationshipScore, -100f, 100f);
        timeOfLastInteraction = Time.time;
    }
}


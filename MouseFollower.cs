using UnityEngine;

public class MouseFollower : MonoBehaviour
{
    [Tooltip("오브제가 마우스를 따라가는 속도입니다.")]
    public float moveSpeed = 8f;

    [Header("Agent Interaction Settings")]
    [Tooltip("상호작용 시 Agent에서 호출할 함수의 이름들")]
    string[] interactionFunctionNames = new string[] {"non", "TakeDamage" , "GiveFood", "ReactToTrick" };

    [Tooltip("TakeDamage 함수에 전달할 공격력")]
    public float attackDamage = 10f;
    float petHealAmount =10f;
    private Camera mainCamera;
    private bool isAttackMode = false;
    private SpriteRenderer spriteRenderer;
    public int n=0;
    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>(); // 색상 변경을 위해 SpriteRenderer 가져오기
    }

    void Update()
    {
        // 1. 마우스 위치를 월드 좌표로 변환
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = transform.position.z;

        // 마우스 위치로 계속 이동
        transform.position = Vector3.MoveTowards(transform.position, mousePosition, moveSpeed * Time.deltaTime);

        // 2. 우클릭으로 공격 모드 활성화/비활성화
        if (Input.GetMouseButtonDown(1)) // 우클릭 누를 때
        {
            n = 1;
            if (spriteRenderer != null) spriteRenderer.color = Color.red; // 공격 모드일 때 빨간색으로 변경
        }/*else if (Input.GetButtonDown("space"))
        {
            n = 2;
        }
        else
        {
            n = 0;
        }*/
        if (Input.GetMouseButtonUp(1)) // 우클릭 뗄 때
        {
            isAttackMode = false;
            n = 0;
            if (spriteRenderer != null) spriteRenderer.color = Color.white; // 원래 색상으로 복귀
        }
    }

    /// <summary>
    /// 다른 Collider2D와 충돌했을 때 호출됩니다.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 공격 모드가 아니거나, 부딪힌 상대가 NewBehaviourScript를 가지고 있지 않으면 무시
      

        Debug.Log(collision.gameObject.name + interactionFunctionNames[n]);

        // interactionFunctionNames 배열에 있는 모든 함수를 호출 시도
      //  foreach (string functionName in interactionFunctionNames)
        //{
            // SendMessage를 사용해 이름으로 함수를 호출하고, attackDamage 값을 전달합니다.
         //float valueToSend = (n == 1) ? attackDamage : petHealAmount;
        float valueToSend = (n == 1) ? attackDamage : petHealAmount;
        collision.gameObject.SendMessage(interactionFunctionNames[n], valueToSend, SendMessageOptions.DontRequireReceiver);
        if (n==1)
        {
        //    collision.Rigidbody2D.AddForce(Vector2.toward, ForceMode.Impulse);


        }

        // 한 번 공격 후 공격 모드 비활성화 (연속적인 공격 방지)
     //   isAttackMode = false;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
    }
}
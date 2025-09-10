using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class langh : MonoBehaviour
{
    private Camera mainCamera; // 메인 카메라를 저장할 변수
    public float distance;
    
 
    // 움직임 유형을 저장할 지역 변수
    string _movementType;

    // 이전 프레임의 거리를 저장할 변수
    private float _previousDistance;

    // "움직이지 않음"으로 간주할 최소 거리 변화량
    [SerializeField] private float movementThreshold = 0.01f;
    NewBehaviourScript mcu;
    void Start()
    {
        mcu =GetComponent<NewBehaviourScript>();
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found! Please make sure your camera is tagged as 'MainCamera'.");
            return;
        }

        // 시작 시 초기 거리 설정
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _previousDistance = Vector3.Distance(transform.position, mouseWorldPosition);
        _movementType = "움직이지 않음(정적)";
    }

    void Update()
    {
        if (mainCamera == null) return;
        string n_movementType;
        // 현재 마우스와 오브젝트 간의 거리 계산
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0));
        float currentDistance = Vector3.Distance(transform.position, mouseWorldPosition);
       
        // 이전 거리와 현재 거리를 비교하여 움직임 유형 감지
        if (Mathf.Abs(currentDistance - _previousDistance) < movementThreshold)
        {
            n_movementType = "움직이지 않음(정적)";
        }
        else if (currentDistance < _previousDistance)
        {
            n_movementType = "다가옴(동적)";
        }
        else // currentDistance > _previousDistance
        {
            n_movementType = "멀어짐(동적)";
        }

        // 다음 프레임을 위해 현재 거리를 이전 거리로 업데이트
        _previousDistance = currentDistance;
        distance = _previousDistance;
        if (n_movementType!=_movementType)
        {
            _movementType = n_movementType;
            //mcu.record(_movementType);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
//단기적인 자극에 대한 반응 ex-아무리 사이가 좋아도 맞으면 한동안 회피를 하는 상태(negatuve on)가 됨, 어느정도 학습이 된다면 
using UnityEngine;

public class tempmenoreact : MonoBehaviour
{
    [Tooltip("오브제가 마우스를 따라가는 속도입니다.")]
    public float moveSpeed = 5f;

    [Tooltip("오브제가 마우스와 유지하려는 최소 거리입니다. 이 거리 안으로 들어오면 오브제는 멈춥니다.")]
    public float followDistance = 2f;

    private Camera mainCamera;
    public float distanceToMouse;
    public bool negative;
    public bool positive;

    void Start()
    {
        // 성능을 위해 메인 카메라를 캐시합니다.
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. 마우스 위치를 월드 좌표로 변환합니다.
        // Z 값은 오브제의 현재 Z 값과 동일하게 설정하여 2D 평면을 유지합니다.
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.y - transform.position.y));
        mousePosition.z = transform.position.z; // 2D 환경을 위해 Z축 고정

        // 2. 마우스와 오브제 사이의 거리를 계산합니다.
        distanceToMouse = Vector3.Distance(transform.position, mousePosition);

        // 3. 거리가 설정된 followDistance보다 클 경우에만 오브제를 이동시킵니다.
        if (distanceToMouse > followDistance && negative == false)
        {
            // 목표 위치(마우스 위치)를 향해 부드럽게 이동합니다.
            transform.position = Vector3.MoveTowards(transform.position, mousePosition, moveSpeed * Time.deltaTime);
        }
        if (distanceToMouse < followDistance && positive == false)
        {
            // 목표 위치(마우스 위치)를 향해 부드럽게 이동합니다.
            transform.position = Vector3.MoveTowards(transform.position, mousePosition, moveSpeed * Time.deltaTime * -1f);
        }
        // 거리가 followDistance보다 작거나 같으면, 오브제는 움직이지 않습니다.
    }
}

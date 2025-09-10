using System.Collections;
using System.Collections.Generic;
//�ܱ����� �ڱؿ� ���� ���� ex-�ƹ��� ���̰� ���Ƶ� ������ �ѵ��� ȸ�Ǹ� �ϴ� ����(negatuve on)�� ��, ������� �н��� �ȴٸ� 
using UnityEngine;

public class tempmenoreact : MonoBehaviour
{
    [Tooltip("�������� ���콺�� ���󰡴� �ӵ��Դϴ�.")]
    public float moveSpeed = 5f;

    [Tooltip("�������� ���콺�� �����Ϸ��� �ּ� �Ÿ��Դϴ�. �� �Ÿ� ������ ������ �������� ����ϴ�.")]
    public float followDistance = 2f;

    private Camera mainCamera;
    public float distanceToMouse;
    public bool negative;
    public bool positive;

    void Start()
    {
        // ������ ���� ���� ī�޶� ĳ���մϴ�.
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 1. ���콺 ��ġ�� ���� ��ǥ�� ��ȯ�մϴ�.
        // Z ���� �������� ���� Z ���� �����ϰ� �����Ͽ� 2D ����� �����մϴ�.
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, mainCamera.transform.position.y - transform.position.y));
        mousePosition.z = transform.position.z; // 2D ȯ���� ���� Z�� ����

        // 2. ���콺�� ������ ������ �Ÿ��� ����մϴ�.
        distanceToMouse = Vector3.Distance(transform.position, mousePosition);

        // 3. �Ÿ��� ������ followDistance���� Ŭ ��쿡�� �������� �̵���ŵ�ϴ�.
        if (distanceToMouse > followDistance && negative == false)
        {
            // ��ǥ ��ġ(���콺 ��ġ)�� ���� �ε巴�� �̵��մϴ�.
            transform.position = Vector3.MoveTowards(transform.position, mousePosition, moveSpeed * Time.deltaTime);
        }
        if (distanceToMouse < followDistance && positive == false)
        {
            // ��ǥ ��ġ(���콺 ��ġ)�� ���� �ε巴�� �̵��մϴ�.
            transform.position = Vector3.MoveTowards(transform.position, mousePosition, moveSpeed * Time.deltaTime * -1f);
        }
        // �Ÿ��� followDistance���� �۰ų� ������, �������� �������� �ʽ��ϴ�.
    }
}

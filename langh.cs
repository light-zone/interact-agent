using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class langh : MonoBehaviour
{
    private Camera mainCamera; // ���� ī�޶� ������ ����
    public float distance;
    
 
    // ������ ������ ������ ���� ����
    string _movementType;

    // ���� �������� �Ÿ��� ������ ����
    private float _previousDistance;

    // "�������� ����"���� ������ �ּ� �Ÿ� ��ȭ��
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

        // ���� �� �ʱ� �Ÿ� ����
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _previousDistance = Vector3.Distance(transform.position, mouseWorldPosition);
        _movementType = "�������� ����(����)";
    }

    void Update()
    {
        if (mainCamera == null) return;
        string n_movementType;
        // ���� ���콺�� ������Ʈ ���� �Ÿ� ���
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0));
        float currentDistance = Vector3.Distance(transform.position, mouseWorldPosition);
       
        // ���� �Ÿ��� ���� �Ÿ��� ���Ͽ� ������ ���� ����
        if (Mathf.Abs(currentDistance - _previousDistance) < movementThreshold)
        {
            n_movementType = "�������� ����(����)";
        }
        else if (currentDistance < _previousDistance)
        {
            n_movementType = "�ٰ���(����)";
        }
        else // currentDistance > _previousDistance
        {
            n_movementType = "�־���(����)";
        }

        // ���� �������� ���� ���� �Ÿ��� ���� �Ÿ��� ������Ʈ
        _previousDistance = currentDistance;
        distance = _previousDistance;
        if (n_movementType!=_movementType)
        {
            _movementType = n_movementType;
            //mcu.record(_movementType);
        }
    }
}
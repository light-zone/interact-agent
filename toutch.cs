using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class toutch : MonoBehaviour
{

    // ������ �ʿ��� ����
    MouseFollower ms;
   
    public bool del;
    public bool negative;
   // public bool positive;

    // �ʿ��� ������Ʈ
    //�ܱ����� �ڱؿ� ���� ���� ex-�ƹ��� ���̰� ���Ƶ� ������ �ѵ��� ȸ�Ǹ� �ϴ� ����(negatuve on)�� ��, ������� �н��� �ȴٸ� 
 //   private Rigidbody2D rb;
    string rec = "";

    int a = 0;
    //NewBehaviourScript mcu;
    void Start()
    {
   //     mcu = GetComponent<NewBehaviourScript>();
        // Rigidbody2D ������Ʈ ��������
     //   rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        if (a>11)
        {

        }
        else if(a!=0)
        {

        }  
    }
    public void nag(float t) {
        if (t<1.5f) {
            negative = true;
            //�÷�
            a++;
        }
    }


    public void inter()
    {
        if (del !=true) {
            // ���콺 Ŭ�� �� �ڷ�ƾ ����
            StartCoroutine(BounceWithDelay());
        }
    }

    private IEnumerator BounceWithDelay()
    {
        // ������ ������ �ð� ���
     
      //  mcu.currentHealth--;
        del = true;
        // ������ �ð���ŭ ���
        yield return new WaitForSeconds(0.5f);
        del = false;


    }
    
   
}//������ �߰� ��?

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class toutch : MonoBehaviour
{

    // 점프에 필요한 변수
    MouseFollower ms;
   
    public bool del;
    public bool negative;
   // public bool positive;

    // 필요한 컴포넌트
    //단기적인 자극에 대한 반응 ex-아무리 사이가 좋아도 맞으면 한동안 회피를 하는 상태(negatuve on)가 됨, 어느정도 학습이 된다면 
 //   private Rigidbody2D rb;
    string rec = "";

    int a = 0;
    //NewBehaviourScript mcu;
    void Start()
    {
   //     mcu = GetComponent<NewBehaviourScript>();
        // Rigidbody2D 컴포넌트 가져오기
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
            //컬러
            a++;
        }
    }


    public void inter()
    {
        if (del !=true) {
            // 마우스 클릭 시 코루틴 시작
            StartCoroutine(BounceWithDelay());
        }
    }

    private IEnumerator BounceWithDelay()
    {
        // 무작위 딜레이 시간 계산
     
      //  mcu.currentHealth--;
        del = true;
        // 딜레이 시간만큼 대기
        yield return new WaitForSeconds(0.5f);
        del = false;


    }
    
   
}//누르기 추가 ㄱ?

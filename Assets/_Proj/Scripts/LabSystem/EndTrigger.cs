using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private BoxCollider collider;
    private FinalCount final; //완주 시 게임 종료 알리는 카운트 스크립트        
    private bool isFinished = false;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
       
    }

    private void Start()
    {
        final = FindAnyObjectByType<FinalCount>();


        if (collider != null)
        {
            collider.enabled = false;
        }        
    }

   public void ActiveTrigger()
    {
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("EndTrigger 활성화");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFinished) return;
        else if (!isFinished)
        {
            isFinished = true;
            Debug.Log("완주!");

            Rigidbody rb = other.GetComponent<Rigidbody>();           
            rb.drag = 20;
            rb.angularDrag = 20;
            rb.isKinematic = true;

            if (other.CompareTag("Player"))
            {               
                CarController player = other.GetComponent<CarController>();                
                player.moveInput = 0;
                player.steerInput = 0;    
            }
            else if (other.CompareTag("AIPlayer"))
            {
                AICarController ai = other.GetComponent<AICarController>();
                ai.moveInput = 0;
                ai.steerInput = 0;
                
            }

            final.Finish();
        }
    }

}

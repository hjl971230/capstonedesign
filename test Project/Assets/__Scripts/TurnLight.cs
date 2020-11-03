using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLight : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.back * 3;
        if (Bartok.CURRENT_PLAYER == null) return;
        transform.position += Bartok.CURRENT_PLAYER.handSlotdef.pos;
    }
}

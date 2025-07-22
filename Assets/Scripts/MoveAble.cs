using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}

public class MoveAble : MonoBehaviour
{
    [SerializeField]
    private Collider2D collider;

    public Transform triggerObject;

    [Header("方向选择")]
    public Direction v_direction;

    [Header("移动位置")]
    [Range(0f, 6f)]
    public float distance;
    private bool isTigger = false;

    private Vector2[] directions = new Vector2[4]
    {
        Vector2.up,
        Vector2.down,
        Vector2.left,
        Vector2.right
    };

    void Start()
    {
        collider = GetComponent<Collider2D>();
        if (collider is null)
        {
            Debug.Log("do not have collider");
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {

        if (other.transform.tag == "Player" && !isTigger)
        {
            //TODO 获取玩家大小类型  
            int type = other.transform.GetComponent<PlayerController>().Size;

            if (type == 2)
            {
                Vector2 new_dir = directions[(int)v_direction] * distance;
                Vector3 new_V3_offset = new Vector3(new_dir.x, new_dir.y, 0f);
                if (triggerObject != null)
                    triggerObject.position += new_V3_offset;
                else
                    transform.position += new_V3_offset;
                isTigger = true;
            }

        }


    }




}

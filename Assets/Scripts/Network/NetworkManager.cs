using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 获取所有其他玩家的GameObject
    /// </summary>
    public List<GameObject> GetOtherPlayers()
    {
        List<GameObject> others = new List<GameObject>();
        PhotonView[] views = GameObject.FindObjectsOfType<PhotonView>();
        foreach (var view in views)
        {
            if (!view.IsMine && view.Owner != null && !view.Owner.IsLocal)
            {
                others.Add(view.gameObject);
            }
        }
        return others;
    }
} 
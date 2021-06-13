using Linux.Net;
using UnityEngine;

public class UnityNetwork : MonoBehaviour
{
    public Hub Hub;

    // Start is called before the first frame update
    void Start()
    {
        Hub = new Hub();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public class Tg_Look : MonoBehaviour
{
    public Transform objectToLookAt;

    public UnityEvent aDeclencher;
    [Range(0.3f, 0.9f)]
    public float sensibility = 0.7f;
    private Vector3 camToObj;
    private Camera cam;
    private Emile_Main avatar;
    private BoxCollider bc;
    private Color boxColor;
    public Color wireColor;
    private Color selectedBoxColor;
    public Color selectedWireColor;

    [SerializeField]
    public bool triggersOnce = false;

    // Start is called before the first frame update

    private void Start()
    {
        cam = Camera.main;
    }
    private void OnTriggerEnter(Collider other)
    {
        avatar = other.GetComponent<Emile_Main>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (avatar == null) return;

        camToObj = objectToLookAt.position - cam.transform.position;
        float dotValue = Vector3.Dot(camToObj.normalized, cam.transform.forward);
        if (dotValue >= sensibility)
        {
            aDeclencher?.Invoke();
            if (triggersOnce) Destroy(this.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (bc == null)
        {
            bc = GetComponent<BoxCollider>();
            if (bc == null) return;
            bc.isTrigger = true;
        }
        boxColor = wireColor;
        boxColor.a = wireColor.a * 0.3f;

        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        Gizmos.color = boxColor;
        Gizmos.DrawCube(transform.position, transform.localScale);

    }

    private void OnDrawGizmosSelected()
    {
        if (bc == null)
        {
            bc = GetComponent<BoxCollider>();
            if (bc == null) return;

        }
        selectedBoxColor = selectedWireColor;
        selectedBoxColor.a = selectedWireColor.a * 0.3f;
        Gizmos.color = selectedWireColor;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        Gizmos.color = selectedBoxColor;
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}

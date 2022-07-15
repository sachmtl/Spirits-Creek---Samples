using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

public class Tg_Walkers : MonoBehaviour
{
    private BoxCollider bc;
    [SerializeField]
    public Color wireColor = new Color(1f, 0, 0, 1f);
    private Color boxColor;
    [SerializeField]
    public Color selectedWireColor = new Color(1f, 0f, 1f, 1f);
    private Color selectedBoxColor;
    [SerializeField]
    public UnityEvent TriggerEnter;
    [SerializeField]
    public UnityEvent TriggerExit;
    [SerializeField]
    public UnityEvent TriggerStay;

    [SerializeField]
    public bool isCustomColor = false;
    [SerializeField]
    public bool triggersOnce = true;
    [SerializeField]
    public bool onEnter = false;
    [SerializeField]
    public bool onExit = false;
    [SerializeField]
    public bool onStay = false;
    [SerializeField]
    public bool hasText;
    [SerializeField]
    public string triggerText;
    [SerializeField]
    public Color textColor = Color.black;
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

    private void OnTriggerEnter(Collider other)
    {
        if (!onEnter) return;
        if (other.TryGetComponent<Walker_Main>(out Walker_Main a))
        {
            TriggerEnter?.Invoke();
            if (triggersOnce) Destroy(this.gameObject);
        }

    }
    private void OnTriggerExit(Collider other)
    {

        if (!onExit) return;
        if (other.TryGetComponent<Walker_Main>(out Walker_Main a))
        {
            TriggerExit?.Invoke();
            if (triggersOnce) Destroy(this.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!onStay) return;
        if (other.TryGetComponent<Walker_Main>(out Walker_Main a))
        {
            TriggerStay?.Invoke();
            if (triggersOnce) Destroy(this.gameObject);
        }
    }
}

using System.Collections;
using TMPro;
using UnityEngine;

public class Customer : MonoBehaviour
{
    public CustomerSO data;
    public float moveSpeed = 1.5f;

    [Header("UI")]
    public TMP_Text dialogueText;
    public Transform dialoguePivot;

    public bool IsServed { get; private set; }

    public void Init(CustomerSO customerData)
    {
        data = customerData;
        dialogueText.text = data.orderDialogue;
    }

    private void Update()
    {
        if (dialoguePivot != null && Camera.main != null)
            dialoguePivot.LookAt(Camera.main.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsServed) return;

        ShakerInput si = other.GetComponent<ShakerInput>();
        if (si == null || si.liquidData != data.requestedDrink) return;

        IsServed = true;
        Destroy(other.gameObject);
        dialogueText.text = data.satisfiedDialogue;
    }

    public IEnumerator WalkTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            Vector3 dir = target - transform.position;
            if (dir.sqrMagnitude > 0.001f)
                transform.forward = dir.normalized;
            yield return null;
        }
        transform.position = target;
    }
}

using System.Collections;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    [Header("Clientes")]
    public CustomerSO[] customers;
    public GameObject customerPrefab;

    [Header("Waypoints")]
    public Transform spawnLeft;
    public Transform counter;
    public Transform spawnRight;

    [Header("Timings")]
    public float delayBetweenCustomers = 1.5f;

    private int _index;

    private void Start() => SpawnNext();

    private void SpawnNext()
    {
        if (_index >= customers.Length)
        {
            Debug.Log("[CustomerManager] Todos los clientes atendidos.");
            return;
        }

        GameObject go = Instantiate(customerPrefab, spawnLeft.position, Quaternion.identity);
        Customer customer = go.GetComponent<Customer>();
        customer.Init(customers[_index++]);
        StartCoroutine(RunCustomer(customer));
    }

    private IEnumerator RunCustomer(Customer customer)
    {
        yield return StartCoroutine(customer.WalkTo(counter.position));

        yield return new WaitUntil(() => customer.IsServed);

        yield return StartCoroutine(customer.WalkTo(spawnRight.position));
        Destroy(customer.gameObject);

        yield return new WaitForSeconds(delayBetweenCustomers);
        SpawnNext();
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomer", menuName = "Customers/Customer")]
public class CustomerSO : ScriptableObject
{
    public string customerName;
    public LiquidSO requestedDrink;

    [TextArea] public string orderDialogue;
    [TextArea] public string satisfiedDialogue;
}

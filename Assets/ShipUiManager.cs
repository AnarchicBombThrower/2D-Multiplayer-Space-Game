using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipUiManager : MonoBehaviour
{
    public RectTransform interactableComponentHealthBarParent;
    public GameObject interactableComponentHealthBarPrefab;
    private Dictionary<ShipInteractableComponent, Image> shipInteractableComponentsHealthBars = new Dictionary<ShipInteractableComponent, Image>();
    const float HEALTH_BAR_FLOAT_ABOVE_DISTANCE = 0.7f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (ShipInteractableComponent componentSync in shipInteractableComponentsHealthBars.Keys)
        {
            syncUiPositionToComponentPosition(componentSync);
        }
    }

    public void displayShip(Ship toDisplay)
    {
        ShipInteractableComponent[] components = toDisplay.getShipComponents();

        foreach (ShipInteractableComponent component in components)
        {
            setupComponentUi(component);
        }
    }

    public void hideShip(Ship toHide)
    {
        ShipInteractableComponent[] components = toHide.getShipComponents();

        foreach (ShipInteractableComponent component in components)
        {
            removeComponentUi(component);
        }
    }

    void setupComponentUi(ShipInteractableComponent componentToSetup)
    {
        Image componentImage = Instantiate(interactableComponentHealthBarPrefab, interactableComponentHealthBarParent).GetComponent<Image>();
        shipInteractableComponentsHealthBars.Add(componentToSetup, componentImage);
        syncUiPositionToComponentPosition(componentToSetup);
    }

    void removeComponentUi(ShipInteractableComponent componentToRemove)
    {
        Destroy(shipInteractableComponentsHealthBars[componentToRemove].gameObject);
        shipInteractableComponentsHealthBars.Remove(componentToRemove); 
    }

    private void syncUiPositionToComponentPosition(ShipInteractableComponent component)
    {
        Image toSync = shipInteractableComponentsHealthBars[component];
        toSync.transform.position = component.transform.position + new Vector3(0, HEALTH_BAR_FLOAT_ABOVE_DISTANCE);
        //toSync.transform.rotation = component.transform.rotation;
    }
}

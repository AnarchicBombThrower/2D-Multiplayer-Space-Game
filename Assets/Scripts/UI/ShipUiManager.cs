using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.Arm;

public class ShipUiManager : MonoBehaviour
{
    public RectTransform interactableComponentHealthBarParent;
    [SerializeField]
    private Image jumpChargeBar;
    [SerializeField]
    private Image hullStrengthBar;
    public GameObject interactableComponentHealthBarPrefab;
    private Dictionary<ShipInteractableComponent, Image> shipInteractableComponentsHealthBars = new Dictionary<ShipInteractableComponent, Image>();
    const float HEALTH_BAR_FLOAT_ABOVE_DISTANCE = 0.7f;
    private float shipMaxJumpCharge;
    private int shipMaxHullStrength;

    void Update()
    {
        foreach (ShipInteractableComponent componentSync in shipInteractableComponentsHealthBars.Keys)
        {
            syncUiPositionToComponentPosition(componentSync);
        }
    }

    public void removeComponentsFromMemory()
    {
        foreach (Image image in shipInteractableComponentsHealthBars.Values)
        {
            Destroy(image);
        }

        shipInteractableComponentsHealthBars = new Dictionary<ShipInteractableComponent, Image>();
    }

    public void localPlayerOnShip(Ship shipOn)
    {
        jumpChargeBar.gameObject.SetActive(true);
        hullStrengthBar.gameObject.SetActive(true);
        shipOn.subscribeToJumpChargeValueChangeCallback(setJumpChargeBarFillAmountTo);
        shipMaxJumpCharge = shipOn.getShipMaxCharge();
        shipOn.subscribeToHullStrengthValueChangeCallback(setHullStrengthBarFillAmountTo);
        shipMaxHullStrength = shipOn.getShipMaxHullStrength();
        setJumpChargeBarFillAmountTo(0, shipOn.getJumpCharge());
        setHullStrengthBarFillAmountTo(0, shipOn.getHullStrength());
    }

    public void setJumpChargeBarFillAmountTo(float prev, float to)
    {
        jumpChargeBar.fillAmount = to / shipMaxJumpCharge;
    }

    public void setHullStrengthBarFillAmountTo(int prev, int to)
    {
        hullStrengthBar.fillAmount = (float)to / (float)shipMaxHullStrength;
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
        toSync.fillAmount = component.getHealthAsFraction();
    }
}

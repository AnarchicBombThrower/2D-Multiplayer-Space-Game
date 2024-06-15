using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ShipAIControl : MonoBehaviour
{
    private Ship ourShip;
    private List<AIControl> AIActorsOnShip = new List<AIControl>();
    private List<AIControl> freeActors = new List<AIControl>();
    private List<ShipInteractableComponent> brokenComponentsToAssign = new List<ShipInteractableComponent>();
    private bool assignSteering = true;
    private bool assignScanner = true;
    private bool assignGun = true;

    // Start is called before the first frame update
    void Start()
    {
        ourShip = GetComponent<Ship>();
        ourShip.getSteeringComponent().subscribeToBrokenStatusSetCallback(steeringComponentBrokeStatus);
        ourShip.getGunComponent().subscribeToBrokenStatusSetCallback(gunComponentBrokeStatus);
        ourShip.getScannerComponent().subscribeToBrokenStatusSetCallback(scannerComponentBrokeStatus);
    }

    private void Update()
    {
        while (freeActors.Count > 0)
        {
            AIControl freeActor = freeActors[0];

            if (brokenComponentsToAssign.Count > 0)
            {
                freeActor.assignRepairComponent(brokenComponentsToAssign[0]);
                brokenComponentsToAssign.RemoveAt(0);
            }
            else if (assignSteering)
            {
                freeActor.assignSteeringComponent(ourShip.getSteeringComponent());
                assignSteering = false;
            }
            else if (assignGun)
            {
                freeActor.assignGunComponent(ourShip.getGunComponent());
                assignGun = false;
            }
            else if (assignScanner)
            {
                freeActor.assignScannerComponent(ourShip.getScannerComponent());
                assignScanner = false;
            }
            else
            {
                return;
            }

            AIActorBusy(freeActor);
        }
    }

    private void steeringComponentBrokeStatus(bool from, bool to)
    {
        if (to == true)
        {
            brokenComponentsToAssign.Add(ourShip.getSteeringComponent());
        }
        else
        {
            assignSteering = true;
        }
    }

    private void gunComponentBrokeStatus(bool from, bool to)
    {
        if (to == true)
        {
            brokenComponentsToAssign.Add(ourShip.getGunComponent());
        }
        else
        {
            assignGun = true;
        }
    }

    private void scannerComponentBrokeStatus(bool from, bool to)
    {
        if (to == true)
        {
            brokenComponentsToAssign.Add(ourShip.getScannerComponent());
        }
        else
        {
            assignScanner = true;
        }
    }

    public void addAIActor(AIControl AIcontrol)
    {
        AIActorsOnShip.Add(AIcontrol);
        freeActors.Add(AIcontrol);
    }

    public void AIActorFree(AIControl AIcontrol, bool failedTask)
    {
        if (freeActors.Contains(AIcontrol))
        {
            return;
        }

        freeActors.Add(AIcontrol);
    }

    private void AIActorBusy(AIControl AIcontrol)
    {
        freeActors.Remove(AIcontrol);
    }

    public void failedToRepairComponent(ShipInteractableComponent failed)
    {
        brokenComponentsToAssign.Add(failed);
    }

    public Ship getShipShootPriority()
    {
        return ShipManager.getPlayersShip();
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AIControl : Controller
{
    private ShipAIControl shipAiControl;
    private Ship shipOn;
    private List<Priority> priortiesInQueue = new List<Priority>();
    public delegate void componentMounted(ShipMountableComponent component);
    private componentMounted componentMountedCallback;
    const float ANGLE_SHOOT_LEEWAY = 20;
    const float ANGLE_DRIVE_LEEWAY = 30;
    const float ANGLE_DONT_ROTATE_LEEWAY = 5;
    const float SHIP_CHASE_DISTANCE = 12;
    const float STAY_WITHIN_DISTANCE_OF_COMPONENT_CONTINUALLY_MOVE = 0.2f;
    const float TRAVEL_TIME_FUDGE = 2;
    const float SHOT_LEADING_FUDGE = 0.8f;

    public void subscribeToMountedCallback(componentMounted subscribe)
    {
        componentMountedCallback += subscribe;
    }

    public void unsubscribeToMountedCallback(componentMounted unsubscribe)
    {
        componentMountedCallback -= unsubscribe;
    }

    abstract class Priority
    {
        protected AIControl controllerControlling;
        protected Actor actorControlling;

        public Priority(AIControl controller, Actor actor)
        {
            controllerControlling = controller;
            actorControlling = actor;
        }

        public abstract void fixedExecute();

        public void complete()
        {
            controllerControlling.priortyComplete();
        }

        public void fail()
        {
            controllerControlling.priorityFailed();
        }
    }

    class MoveToShipComponentPriority : Priority
    {
        private ShipInteractableComponent moveTo;
        private bool continuallyMove;
        private bool alreadyHere = false;

        public MoveToShipComponentPriority(AIControl controller, Actor actor, ShipInteractableComponent moveToward, bool continually) : base(controller, actor)
        {
            if (actor.canInteractWith(moveToward))
            {
                alreadyHere = true;
            }

            moveTo = moveToward;
            continuallyMove = continually;

            if (continually == false)
            {
                actor.subscribeToCanInteractWithCallback(actorAbleToInteractWithComponent);
            }     
        }

        public override void fixedExecute()
        { 
            if (alreadyHere)
            {
                done();
            }

            if (continuallyMove)
            {
                if (Vector2.Distance(moveTo.transform.position, actorControlling.transform.position) < STAY_WITHIN_DISTANCE_OF_COMPONENT_CONTINUALLY_MOVE)
                {
                    return;
                }
            }

            float xDifference = moveTo.transform.position.x - actorControlling.transform.position.x;
            float yDifference = moveTo.transform.position.y - actorControlling.transform.position.y;

            if (xDifference > 0) //if we need to go to right and we have less velocity than distance left add more force
            {
                actorControlling.addForceRight();
            }
          
            if (xDifference < 0) //vice versa
            {
                actorControlling.addForceLeft();
            }

            if (yDifference > 0) //if we need to go to up and we have less velocity than distance left add more force
            {
                actorControlling.addForceUp();
            }

            if (yDifference < 0) //vice versa
            {
                actorControlling.addForceDown();
            }
        }

        public void actorAbleToInteractWithComponent(ShipInteractableComponent component)
        {
            if (component == moveTo)
            {
                done();
            }
        }

        private void done()
        {
            actorControlling.unsubscribeToCanInteractWithCallback(actorAbleToInteractWithComponent);
            complete();
        }
    }

    class GetOnShipComponentPriority : Priority
    {
        ShipMountableComponent getOn;

        public GetOnShipComponentPriority(AIControl controller, Actor actor, ShipMountableComponent _getOn) : base(controller, actor)
        {
            getOn = _getOn;
            controller.subscribeToMountedCallback(mountedOnComponent);
        }

        public override void fixedExecute()
        {
            actorControlling.interactWithSpecific(getOn);
        }

        public void mountedOnComponent(ShipMountableComponent component)
        {
            if (component == getOn)
            {
                controllerControlling.unsubscribeToMountedCallback(mountedOnComponent);
                complete();
            }
        }
    }

    class ShootGunAtShipPriority : Priority
    {
        GunComponent toShoot;

        public ShootGunAtShipPriority(AIControl controller, Actor actor, GunComponent shoot) : base(controller, actor)
        {
            toShoot = shoot;
        }

        public override void fixedExecute()
        {
            Ship ship = controllerControlling.getShootPriority();

            if (ship == null) { return; }

            float distanceBetweenGunAndTargetShip = Vector2.Distance(toShoot.transform.position, ship.transform.position);
            float estimateTravelTime = toShoot.getMissileSpeed() / distanceBetweenGunAndTargetShip;

            Vector2 shootAt;

            if (estimateTravelTime > TRAVEL_TIME_FUDGE)
            {
                shootAt = (Vector2)ship.transform.position + ship.getRigidbodyVelocity() * estimateTravelTime * SHOT_LEADING_FUDGE;
            }
            else
            {
                shootAt = ship.transform.position;
            }

            float angle = Vector2.SignedAngle(toShoot.transform.up, shootAt - (Vector2)toShoot.transform.position);

            if (angle < 0)
            {
                toShoot.rotateGunRight();
            }
            else
            {
                toShoot.rotateGunLeft();
            }

            if (distanceBetweenGunAndTargetShip > toShoot.getMissileShootDistance())
            {
                toShoot.extendMissileShootDistance();
            }

            if (angle < ANGLE_SHOOT_LEEWAY)
            {
                toShoot.shootMissile();
            }
        }
    }

    class SteerShipPriority : Priority
    {
        private SteeringComponent toSteer;

        public SteerShipPriority(AIControl controller, Actor actor, SteeringComponent steer) : base(controller, actor)
        {
            toSteer = steer;
        }

        public override void fixedExecute()
        {
            Ship opponentShip = controllerControlling.getShootPriority();

            if (opponentShip == null) { return; } 

            Ship ourShip = controllerControlling.getShipOn();

            float distanceToShip = Vector2.Distance(opponentShip.transform.position, ourShip.transform.position);
            float angle = Vector2.SignedAngle(ourShip.transform.up, opponentShip.transform.position - ourShip.transform.position);

            if (angle < -ANGLE_DONT_ROTATE_LEEWAY)
            {
                ourShip.rotateShip(true);
            }
            else if (angle > ANGLE_DONT_ROTATE_LEEWAY)
            {
                ourShip.rotateShip(false);
            }

            if (distanceToShip > SHIP_CHASE_DISTANCE && angle < ANGLE_DRIVE_LEEWAY) //are we close and close to the right angle to start moving forward?
            {
                ourShip.applyThrust();
            }
        }
    }

    class RepairComponentPriority : Priority
    {
        private ShipInteractableComponent toRepair;

        public RepairComponentPriority(AIControl controller, Actor actor, ShipInteractableComponent repair) : base(controller, actor)
        {
            toRepair = repair;
            toRepair.subscribeToBrokenStatusSetCallback(repairingStatusSet);
        }

        public override void fixedExecute()
        {
            if (actorControlling.repairSpecific(toRepair) == false)
            {
                fail();
                controllerControlling.failedToRepairComponent(toRepair);
            }
        }

        public void repairingStatusSet(bool from, bool to)
        {
            if (to)
            {
                complete();
            }
        }
    }

    protected override void onStart()
    {
        
    }

    void Update()
    {
        
    }

    public void assignGunComponent(GunComponent component)
    {
        addToQueue(new MoveToShipComponentPriority(this, ourActor, component, false)); //first priority, move to this component
        addToQueue(new GetOnShipComponentPriority(this, ourActor, component)); //second we get on it!
        addToQueue(new ShootGunAtShipPriority(this, ourActor, component)); //then we shoot it
    }

    public void assignSteeringComponent(SteeringComponent component)
    {
        addToQueue(new MoveToShipComponentPriority(this, ourActor, component, false)); //first priority, move to this component
        addToQueue(new GetOnShipComponentPriority(this, ourActor, component)); //second we get on it!
        addToQueue(new SteerShipPriority(this, ourActor, component)); //then we steer it
    }

    public void assignScannerComponent(ScannerComponent component)
    {
        addToQueue(new MoveToShipComponentPriority(this, ourActor, component, true));
    }

    public void assignRepairComponent(ShipInteractableComponent component)
    {
        print("TESSSTTTTTTTTT");
        addToQueue(new MoveToShipComponentPriority(this, ourActor, component, false));
        addToQueue(new RepairComponentPriority(this, ourActor, component));
    }

    public Ship getShootPriority()
    {
        return shipAiControl.getShipShootPriority();
    }

    private void addToQueue(Priority toAdd)
    {
        priortiesInQueue.Insert(priortiesInQueue.Count, toAdd);
    }

    private void FixedUpdate()
    {
        if (IsHost == false) { return; }

        if (priortiesInQueue.Count == 0)
        {
            shipAiControl.AIActorFree(this, false);
            return;
        }

        priortiesInQueue[0].fixedExecute();     
    }

    public void priortyComplete()
    {
        priortiesInQueue.RemoveAt(0);
    }

    public void priorityFailed()
    {
        priortiesInQueue.Clear();
        shipAiControl.AIActorFree(this, true);
    }

    public void failedToRepairComponent(ShipInteractableComponent component)
    {
        shipAiControl.failedToRepairComponent(component);
    }

    public override void unmounted()
    {
        shipAiControl.AIActorFree(this, true);
    }

    public override void mountedOnSteering(SteeringComponent mountedOn)
    {
        if (componentMountedCallback != null)
        {
            componentMountedCallback(mountedOn);
        }
    }

    public override void mountedOnGun(GunComponent mountedOn)
    {
        if (componentMountedCallback != null)
        {
            componentMountedCallback(mountedOn);
        }
    }

    public override void onShip(Ship nowOn)
    {
        ShipAIControl shipControl = nowOn.GetComponent<ShipAIControl>();

        if (shipControl == null)
        {
            Debug.LogError("AI was put on ship without an AI ship control!");
            return;
        }

        shipOn = nowOn;
        shipAiControl = shipControl;
    }

    public Ship getShipOn()
    {
        return shipOn;
    }

    public override void sightOfShip(Ship nowSeen)
    {

    }

    public override void noSightOfShip(Ship nowUnseen)
    {

    }

}

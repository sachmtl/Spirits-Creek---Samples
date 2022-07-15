using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beast_ST_Patrol : Beast_State
{
    public bool executeChange = true;
    private Vector2 smoothDeltaPos = Vector2.zero;
    private Vector2 velocity = Vector2.zero;

    private float counter = 0f;
    private Beast_Main maBase;
    private Coroutine update;
    private bool patrol = true;
    public bool change = false;
    private bool toTransition = false;
    public bool backFromStun = false;

    public Beast_ST_Patrol(Beast_Main maBase)
    {
        this.maBase = maBase;
    }

    public override IEnumerator Start()
    {
        if (update == null) update = maBase.StartCoroutine(this.Update());
        else
        {
            maBase.StopCoroutine(update);
            update = null;
            update = maBase.StartCoroutine(this.Update());
        }
        patrol = true;
        yield break;
    }
    public override IEnumerator End()
    {
        maBase.anim.SetBool("isPatroling", false);
        if (update != null) maBase.StopCoroutine(update);
        
        yield break;
    }
    public override IEnumerator Update()
    {
        patrol = true;
        while (patrol)
        {
            maBase.agent.destination = maBase.nextWp.position;
            change = maBase.agent.remainingDistance <= 5f ? true : false;
            if(backFromStun)
            {
                change = true;
                executeChange = true;
                backFromStun = false;
            }
            if (change && executeChange)
            {
                change = false;
                maBase.traveledPath = 0.5f;
                maBase.ChangeNextWP();
                maBase.StartCoroutine(this.ResetWPExec());
                if (backFromStun) backFromStun = false;
            }
            yield return new WaitForEndOfFrame();
        }
        yield break;
    }

    public IEnumerator ResetWPExec()
    {
        yield return new WaitForSeconds(2f);
        executeChange = true;
        yield break;
    }

    public override void PlayerSeen(Vector3 target)
    {
        if (update != null) maBase.StopCoroutine(update);
        maBase.chase.target = target;
        maBase.SetState(maBase.chase);
        maBase.anim.SetBool("isPatroling", false);
        return;
    }

    public override IEnumerator NoiseHeard(Vector3 target)
    {
        if (update != null) maBase.StopCoroutine(update);
        maBase.agent.isStopped = true;
        maBase.anim.SetTrigger("tg_IdleBreak");
        yield return new WaitForSeconds(2f);
        maBase.agent.isStopped = false;
        maBase.investigate.target = target;
        patrol = false;
        update = null;
        maBase.SetState(maBase.investigate);
        yield break;
    }

    public override void StartHunting(CheminBeast huntZone)
    {
        return;
    }

    public override void StopHunting()
    {
        if (!maBase.directorData.inLOS)
        {
            maBase.directorData.searchStarted = false;
            if (update != null) maBase.StopCoroutine(update);
            maBase.SetState(maBase.idle);
        }
        return;
    }

    public override void GoToPlayerZone(CheminBeast huntZone)
    {
        if (huntZone == null)
        {
            maBase.Deactivate();
            maBase.directorData.zoneNotFound = true;
            maBase.directorData.searchStarted = false;
            if (update != null) maBase.StopCoroutine(update);
            maBase.SetState(maBase.idle);
            return;
        }

        maBase.monChemin = huntZone;
        maBase.currentWp = huntZone.waypoints[maBase.directorData.FarthestWPInZone(huntZone, maBase.directorData.playerPos)];
        maBase.nextWp = maBase.directorData.FarthestWPInZone(huntZone, maBase.directorData.playerPos) + 1 == huntZone.waypoints.Count ?
                        huntZone.waypoints[0] : huntZone.waypoints[maBase.directorData.FarthestWPInZone(huntZone, maBase.directorData.playerPos) + 1];
        return;
    }

    public override IEnumerator Attack()
    {
        yield break;
    }

    public override void GoToJumpState()
    {
        if (update != null) maBase.StopCoroutine(update);
        toTransition = true;
        maBase.SetState(maBase.transition);
    }

    public override void FlareStun()
    {
        maBase.stunned.previousPatrolDestination = maBase.agent.destination;
        maBase.SetState(maBase.stunned);

        return;
    }

    public override void PropaneStun()
    {
        return;
    }

    public override void GoToFallState()
    {
        if (update != null) maBase.StopCoroutine(update);
        maBase.SetState(maBase.falling);
        return;
    }
}

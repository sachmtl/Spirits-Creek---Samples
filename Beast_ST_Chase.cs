using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beast_ST_Chase : Beast_State
{
    public Vector3 lastKnownLocation;
    public Vector3 target;
    public bool playerLost = false;

    private Beast_Main maBase;
    private Coroutine update;
    private Coroutine attack;
    private float headRotation = 0f;
    private float rotateCounter = 0f;
    private bool triggered = false;


    public Beast_ST_Chase(Beast_Main maBase)
    {
        this.maBase = maBase;
    }

    public override IEnumerator Start()
    {
        maBase.directorData.beastChasing = true;
        MusicStatesManager.instance.BeastChasing();
        maBase.anim.SetBool("isPatroling", false);
        maBase.anim.SetTrigger("tg_Alert");
        yield return new WaitForSeconds(2f);
        if (maBase.angerLevel >= 1f)
        {
            maBase.anim.SetBool("isQuadruped", true);
            maBase.anim.SetBool("isPatroling", false);
            maBase.animSpeedMultiplier -= 0.25f;
        }
        else
        {
            maBase.anim.SetBool("isPatroling", true);
            maBase.anim.SetBool("isQuadruped", false);
            maBase.animSpeedMultiplier -= 0.1f;
        }
        
        playerLost = false;
        maBase.agent.destination = target;
        maBase.visionConeRange = 25f;
        maBase.visionConeAngle = 120f;
        
        update = maBase.StartCoroutine(this.Update());
        yield break;
    }
    public override IEnumerator End()
    {
        maBase.anim.SetBool("isChasing", false);
        maBase.anim.SetBool("isQuadruped", false);
        if (update != null) maBase.StopCoroutine(update);
        maBase.angerLevel += 1f;
        maBase.visionConeRange = 15f;
        maBase.visionConeAngle = 70f;
        maBase.animSpeedMultiplier = 0.8f;
        yield break;
    }
    public override IEnumerator Update()
    {
        while (!playerLost)
        {
            maBase.anim.SetBool("isPatroling", false);
            target = maBase.directorData.playerPos;
            maBase.agent.destination = target;

            if (Vector3.SqrMagnitude(target - maBase.tr.position) <= 4f)
            {
                if (!triggered && maBase.directorData.player.avatar.State != maBase.directorData.player.avatar.dead)
                {
                    attack = maBase.StartCoroutine(this.Attack());
                    triggered = true;
                }
            }
            else
            {
                triggered = false;
                maBase.agent.isStopped = false;
                maBase.anim.SetBool("isChasing", true);
            }
            yield return new WaitForEndOfFrame();
        }
        triggered = false;
        maBase.agent.isStopped = false;
        maBase.agent.destination = lastKnownLocation;
        maBase.directorData.beastChasing = false;
        MusicStatesManager.instance.StartExploring();
        yield return new WaitForSeconds(1f);

        maBase.SetState(maBase.patrol);
        yield break;
    }

    public override void PlayerSeen(Vector3 target)
    {
        this.target = target;
        return;
    }

    public override IEnumerator NoiseHeard(Vector3 target)
    {
        yield break;
    }

    public override void StartHunting(CheminBeast huntZone)
    {
        return;
    }

    public override void StopHunting()
    {
        return;
    }

    public override void GoToPlayerZone(CheminBeast huntZone)
    {
        return;
    }

    public override IEnumerator Attack()
    {
        maBase.agent.isStopped = true;
        maBase.anim.SetTrigger("tg_Grab");
        yield return new WaitForSeconds(1f);
        if (Vector3.SqrMagnitude(target - maBase.tr.position) <= 4f)
        {
            maBase.timeLineManager.tlKillBeast.StartKill();
        }
        else
        {
            playerLost = false;
            maBase.agent.isStopped = false;
        }
        yield break;
    }

    public override void GoToJumpState()
    {
        maBase.SetState(maBase.jump);
    }

    public override void FlareStun()
    {
        maBase.StartCoroutine(BeStunnedByFlare());
        return;
    }

    IEnumerator BeStunnedByFlare()
    {
        maBase.agent.isStopped = true;
        maBase.anim.SetTrigger("tg_stunFlare");

        yield return new WaitForSeconds(4f);

        maBase.agent.isStopped = false;

        yield break;
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

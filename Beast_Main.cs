using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(Beast_SensorsProcessor))]
public class Beast_Main : Beast_BaseStateMachine
{
    public string currentState;

    public TimelineManager timeLineManager;

    public CheminBeast monChemin;
    public SO_DirectorAI directorData;
    public Transform rayTargetForDirector;
    [HideInInspector] public Transform tr;
    [HideInInspector] public Animator anim;
    [HideInInspector] public float traveledPath = 0f;
    [HideInInspector] public Transform currentWp;
    [HideInInspector] public Transform nextWp;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public bool isLooking = false;
    public Beast_VisionCone vc;
    public Beast_Hearing hr;
    public GameObject smrParent;
    public SkinnedMeshRenderer[] smr;
    private int changerChemin;
    private int takeALook;
    [HideInInspector] public CapsuleCollider cc;
    [HideInInspector] public Rigidbody rb;
    private bool navigating = false;

    public Transform rayStartForSpawn;
    public LayerMask lmForSpawn;

    private float test = 0f;
    private double velocityX;
    private double velocityY;
    private Vector3 worldDeltaPos;
    private Vector2 groundDeltaPos;

    [HideInInspector] public bool isTransitioning = false;

    private List<AnimatorClipInfo> animClipInf = new List<AnimatorClipInfo>();

    [HideInInspector] public bool checkForJump = false;

    //Anim dominant layer
    private float counter = 0f;
    private int dominantLayer = 0;

    private Beast_LookAt lookAt;
    private Vector2 smoothDeltaPos = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private Vector2 deltaPosition;

    private RaycastHit hit;
    public LayerMask mask;

    private List<Transform> possibleTargets = new List<Transform>();
    [HideInInspector] public bool activated = false;
    [HideInInspector] public bool transitioning = false;

    [HideInInspector] private bool animEventHold = false;

    public float animSpeedMultiplier = 0.8f;
    public float angerLevel = 0f;
    private float formerAnger = 0f;

    public LayerMask maskForFallState;
    public Transform rayRightFootStart;
    public Transform rayLeftFootStart;
    public Transform rfRayFollow;
    public Transform lfRayFollow;
    private Vector3 rfRayTargetPos;
    private Vector3 lfRayTargetPos;
    private RaycastHit hitRightFoot;
    private RaycastHit hitLeftFoot;
    [HideInInspector] public bool onGround = true;

    #region Vision Parameters
    [HideInInspector] public float visionConeRange = 15f;
    [HideInInspector] public float visionConeAngle = 70f;
    [HideInInspector] public float hearingRange = 25f;
    [HideInInspector] public float proximityDetectionRange = 3f;
    [HideInInspector] public Vector3 eyeLocation;
    [HideInInspector] public Vector3 eyeDirection => transform.forward;

    [HideInInspector] public float cosVisionConeAngle { get; private set; } = 0f;

    private Beast_SensorsProcessor sensorsProcessor;

    #region Tests Navigation (Climb, Vault, etc.)
    private Vector3 bcStart;
    private Vector3 bcDirection;
    private Vector3 bcHalfExtents = new Vector3(2.3f, 2.3f, 2.3f);
    private RaycastHit bcHit;
    private bool processedOML = false;
    #endregion
    #endregion

    #region Beast States
    public Beast_ST_Idle idle;
    public Beast_ST_Investigate investigate;
    public Beast_ST_Chase chase;
    public Beast_ST_ChaseFail chaseFail;
    public Beast_ST_Attack attack;
    public Beast_ST_AttackFail attackFail;
    public Beast_ST_AttackSuccess attackSuccess;
    public Beast_ST_Scream scream;
    public Beast_ST_Scripted scripted;
    public Beast_ST_Spectate spectate;
    public Beast_ST_BreakObject breakObject;
    public Beast_ST_Patrol patrol;
    public Beast_ST_ActivePatrol activePatrol;
    public Beast_ST_Stunned stunned;
    public Beast_ST_Looking looking;
    public Beast_ST_Jump jump;
    public Beast_ST_Transition transition;
    public Beast_ST_Falling falling;
    #endregion

    #region Accesseurs
    public Beast_State State
    {
        get { return state; }
    }
    #endregion

    private void Awake()
    {
        eyeLocation = transform.position;
        cosVisionConeAngle = Mathf.Cos(visionConeAngle * Mathf.Deg2Rad);
        sensorsProcessor = GetComponent<Beast_SensorsProcessor>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
        #region Beast States Init
        idle = new Beast_ST_Idle(this);
        investigate = new Beast_ST_Investigate(this);
        chase = new Beast_ST_Chase(this);
        chaseFail = new Beast_ST_ChaseFail(this);
        attack = new Beast_ST_Attack(this);
        attackFail = new Beast_ST_AttackFail(this);
        attackSuccess = new Beast_ST_AttackSuccess(this);
        scream = new Beast_ST_Scream(this);
        scripted = new Beast_ST_Scripted(this);
        spectate = new Beast_ST_Spectate(this);
        breakObject = new Beast_ST_BreakObject(this);
        patrol = new Beast_ST_Patrol(this);
        activePatrol = new Beast_ST_ActivePatrol(this);
        stunned = new Beast_ST_Stunned(this);
        looking = new Beast_ST_Looking(this);
        jump = new Beast_ST_Jump(this);
        transition = new Beast_ST_Transition(this);
        falling = new Beast_ST_Falling(this);
        #endregion
        
        cc = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        tr = transform;
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        lookAt = GetComponent<Beast_LookAt>();
        agent.updatePosition = false;
        Invoke("InitializeWPs", 0f);
        Invoke("SetAgentSpeed", 1f);
        directorData.rayTargetForLOS = rayTargetForDirector;
        agent.autoTraverseOffMeshLink = false;
        checkForJump = false;
        anim.applyRootMotion = false;
        SetState(idle);
    }

    // Update is called once per frame
    void Update()
    {
        if (activated) {
            if (agent.isOnOffMeshLink && !processedOML) 
            {
                StartCoroutine(DetermineOffMeshLinkBehavior()); 
            }
            eyeLocation = tr.position;
            currentState = state.ToString();
            worldDeltaPos = agent.nextPosition - tr.position;
            groundDeltaPos.x = Vector3.Dot(tr.right, worldDeltaPos);
            groundDeltaPos.y = Vector3.Dot(tr.forward, worldDeltaPos);
            deltaPosition = new Vector2(groundDeltaPos.x, groundDeltaPos.y);
            float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
            smoothDeltaPos = Vector2.Lerp(smoothDeltaPos, deltaPosition, smooth);
            if (Time.deltaTime > 1e-5f) velocity = smoothDeltaPos / Time.deltaTime;
            bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;
            if (state == patrol && !processedOML) anim.SetBool("isPatroling", shouldMove);
            else if (state == patrol && processedOML) anim.SetBool("isPatroling", true);
            anim.SetFloat("VelocityX", velocity.x);
            anim.SetFloat("VelocityZ", velocity.y);

            Vector3 test = new Vector3(rayStartForSpawn.position.x, rayStartForSpawn.position.y - 1f, rayStartForSpawn.position.z + 5f);
            

            if (checkForJump)
            {
                if (Vector3.Distance(tr.position, monChemin.CurrentWP.position) <= 1f)
                {
                    state.GoToJumpState();
                }
            }

            if (angerLevel - formerAnger >= 1f)
            {
                formerAnger = angerLevel;
            }
        }
    }

    public IEnumerator DetermineOffMeshLinkBehavior()
    {
        processedOML = true;
        int omlArea = agent.currentOffMeshLinkData.offMeshLink.area;
        switch (omlArea)
        {
            case 3:
                StartCoroutine(LerpToOMLEnd("tg_ClimbSmall"));
                break;
            case 4:
                StartCoroutine(LerpToOMLEnd("tg_ClimbVault"));
                break;
            case 5:
                StartCoroutine(LerpToOMLEnd("tg_Door"));
                break;
            default:
                break;
        }

        yield break;
    }

    public IEnumerator LerpToOMLEnd(string animToTrigger)
    {
        navigating = true;
        Vector3 start = tr.position;
        Vector3 end = agent.currentOffMeshLinkData.endPos;
        bool lerpDone = false;
        float lerpVal = 0f;
        bool wasPatroling = anim.GetBool("isPatroling");
        bool wasChasing = anim.GetBool("isChasing");
        bool wasQuadru = anim.GetBool("isQuadruped");
        if(wasPatroling) anim.SetBool("isPatroling", false);
        if(wasChasing) anim.SetBool("isChasing", false);
        if(wasQuadru) anim.SetBool("isQuadruped", false);
        cc.enabled = false;
        anim.SetTrigger(animToTrigger);
        Vector3 previousDest = agent.destination;
        agent.destination = tr.position;
        while (!lerpDone)
        {
            rb.MovePosition(Vector3.Lerp(start, end, lerpVal));
            lerpVal += Time.deltaTime;
            if (lerpVal >= 0.99f) lerpDone = true;

            yield return new WaitForEndOfFrame();
        }
        agent.CompleteOffMeshLink();
        if (wasPatroling) anim.SetBool("isPatroling", true);
        if (wasChasing) anim.SetBool("isChasing", true);
        if (wasQuadru) anim.SetBool("isQuadruped", true);
        cc.enabled = true;
        agent.destination = previousDest;
        navigating = false;
        processedOML = false;
        yield break;
    }

    public void GoBackToLastState()
    {
        SetState(previousState);
        return;
    }

    void StopAllAnims()
    {
        anim.SetBool("isPatroling", false);
        anim.SetBool("isChasing", false);
        anim.SetFloat("VelocityX", 0f);
        anim.SetFloat("VelocityZ", 0f);
    }

    Vector3 FillTargetsArrayAndFindClosestTarget(Transform toClimbOrVault)
    {
        int index = 0;
        Vector3 result = Vector3.zero;
        for (int i = 0; i < toClimbOrVault.childCount; i++)
        {
            Transform currentChild = toClimbOrVault.GetChild(i);
            if (!possibleTargets.Contains(currentChild))
            {
                if (currentChild.gameObject.name.Contains("Target"))
                {
                    possibleTargets.Insert(index, currentChild);
                    index++;
                }
            }
        }

        int positionClosest = -1;
        float minDistanceFound = 100f;
        for (int i = 0; i < possibleTargets.Count; i++)
        {
            float currentDist = Vector3.Distance(tr.position, possibleTargets[i].position);
            if(currentDist < minDistanceFound)
            {
                minDistanceFound = currentDist;
                positionClosest = i;
            }
        }

        if (positionClosest != -1)
        {
            result = possibleTargets[positionClosest].position;
            return result;
        }
        else return result;
    }

    private void InitializeWPs()
    {
        if (monChemin != null)
        {
            currentWp = monChemin.CurrentWP;
            nextWp = monChemin.NextWP;
            SetState(idle);
        }
    }
    
    public void ChangeNextWP()
    {
        if (monChemin.NextWP.GetComponent<Waypoint>().type == Waypoint.wpType.transition)
        {
            changerChemin = UnityEngine.Random.Range(1, 3);
            if (changerChemin == 1)
            {
                agent.destination = monChemin.NextWP.GetComponent<Waypoint>().wpDestination.position;
                nextWp = monChemin.NextWP.GetComponent<Waypoint>().wpDestination;
                currentWp = monChemin.NextWP.GetComponent<Waypoint>().wpDestination;
                monChemin = currentWp.GetComponentInParent<CheminBeast>();
            }
        }

        if (monChemin.NextWP.GetComponent<Waypoint>().type == Waypoint.wpType.look)
        {
            isLooking = true;
            takeALook = UnityEngine.Random.Range(1, 3);
            if (takeALook == 1)
            {
                agent.destination = monChemin.NextWP.GetComponent<Waypoint>().wpDestination.position;
                looking.toTreat = monChemin.NextWP.GetComponent<Waypoint>().wpDestination;
                SetState(looking);
            }
        }

        if (monChemin.NextWP.GetComponent<OffMeshLink>() != null)
        {
            checkForJump = true;
        }

        if (isLooking == false)
        {
            patrol.executeChange = false;
            monChemin.UpdateWaypoints(out currentWp, out nextWp);
        }

        
    }

    public void InitializeScriptedPatrol(CheminBeast place)
    {
        if (activated) 
        {
            Deactivate();
        }
        tr.position = place.waypoints[0].position;
        scripted.mode = Beast_ST_Scripted.ScriptedMode.Patrol;
        SetState(scripted);
    }

    public void InitializeScriptedChase()
    {

    }

    private void OnAnimatorMove()
    {
        Vector3 position = anim.rootPosition;
        position.y = agent.nextPosition.y;

		tr.position = position;
		transform.position = agent.nextPosition;
		agent.nextPosition = tr.position;
		if ((anim.deltaPosition / Time.deltaTime).magnitude > 1f) agent.speed = (anim.deltaPosition / Time.deltaTime).magnitude * animSpeedMultiplier;

    }

    public void Deactivate()
    {
        anim.enabled = false;
        agent.enabled = false;
        sensorsProcessor.enabled = false;
        DeactivateRenderers();
        vc.enabled = false;
        hr.enabled = false;
        activated = false;
        rb.useGravity = false;
        angerLevel = 0f;
    }

    public void ToggleGravity()
    {
        rb.useGravity = true;
    }

    public void Activate()
    {
        anim.enabled = true;
        agent.enabled = true;
        cc.enabled = true;
        sensorsProcessor.enabled = true;
        ActivateRenderers();
        vc.enabled = true;
        hr.enabled = true;
        activated = true;
    }

    void DeactivateRenderers()
    {
        for (int i = 0; i < smr.Length; i++)
        {
            smr[i].enabled = false;
        }
    }

    void ActivateRenderers()
    {
        for (int i = 0; i < smr.Length; i++)
        {
            smr[i].enabled = true;
        }
    }

    void FillRdArray()
    {
        int helper = 0;
        for (int i = 0; i < smrParent.transform.childCount; i++)
        {
            if (i == 15 || i == 16)
            {
                for (int ii = 0; ii < smrParent.transform.GetChild(i).childCount; ii++)
                {
                    smr[i + ii] = smrParent.transform.GetChild(i).transform.GetChild(ii).GetComponent<SkinnedMeshRenderer>();
                    if (i == 15) helper = ii;
                    else helper = 0;
                }
                i = i + helper;
            }
            else
            {
                smr[i] = smrParent.transform.GetChild(i).GetComponent<SkinnedMeshRenderer>();
            }
        }
    }

    IEnumerator AnimEventDuration(float seconds, bool toSwitch)
    {
        toSwitch = true;
        yield return new WaitForSeconds(seconds);
        toSwitch = false;
        yield break;
    }

    IEnumerator PlayBurstWallAnim()
    {
        SetState(idle);

        agent.isStopped = true;
        anim.SetTrigger("tg_BurstWall");
        StartCoroutine(this.AnimEventDuration(3f, animEventHold));
        while (animEventHold)
        {
            anim.SetFloat("VelocityX", 0f);
            anim.SetFloat("VelocityZ", 0f);
            anim.SetBool("isPatroling", false);
            anim.SetBool("isChasing", false);

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    public void ReportPossibleTarget(Beast_DetectableTargets caught)
    {
        if(directorData.absYOffset < 2.3f) sensorsProcessor.ReportSeen(caught);
    }

    public void ReportSoundHeard(GameObject source, Vector3 location, SoundCategory category, float intensity)
    {
        sensorsProcessor.ReportSound(gameObject, location, category, intensity);
    }

    public void ReportProximityAlert(Beast_DetectableTargets target)
    {
        sensorsProcessor.ReportProximityAlert(target);
    }

    public void OnSuspicious()
    {
        if(activated) state.NoiseHeard(directorData.playerPos);
    }

    public void OnDetected(GameObject target)
    {
        //if(activated)
    }

    public void OnLostDetection(GameObject target)
    {
        if (activated) chase.playerLost = true;
    }

    public void OnSuspicionLost()
    {
        //if (activated)
    }

    public void OnFullyLost()
    {
        if (activated) chase.playerLost = true;
    }

    public void OnFullyDetected(GameObject target)
    {
        if (activated) state.PlayerSeen(target.transform.position);
    }
}

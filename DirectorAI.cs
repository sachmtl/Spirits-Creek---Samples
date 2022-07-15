using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;

public class DirectorAI : MonoBehaviour
{
    #region Proprietes
    public static DirectorAI instance { get; private set; } = null;

    public SO_DirectorAI dataContainer;
    public double menaceGauge;
    public bool searchStarted;
    public bool inLOS;

    public float amortissement = 2f;

    public SO_JobSystem jobSys;
    public List<Job> jobList;
    private Job testJob;
    private bool startChecks = false;

    public Beast_Main beast;
    public Emile_LOSBeast player;
    private CheminBeast playerZone;
    public Vector3 playerPos;
    public float absYOffset;
    private float playerAIDistance;
    private bool aiTooFar = false;
    private float timeSinceTooFar = 0f;

    public float beastAngerLevel = 0f;

    public bool dontAppear = false;
    public bool overrideMG = false;
    private bool waitForSpawn = false;

    private Coroutine waitSpawn;

    private List<CheminBeast> closestChemin = new List<CheminBeast>();

    private float waitCoutnerAfterCheatKill;
    private bool noSpawning = false;
    #endregion

    #region Fonctions (Mono et custom)

    #region MonoBehaviour
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Multiple DirectorAI Singleton Instances found. Destroying new one" + gameObject.name);
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start()
    {
        Invoke("GetPlayer", 0f);
        if(overrideMG)
        {
            Invoke("GymStart", 2f);
        }
        menaceGauge = 0f;
        dataContainer.menaceGauge = 0f;
    }

    void GymStart()
    {
        if (!dataContainer.zoneNotFound)
        {
            if(beast.State != null) beast.State.StartHunting(playerZone);
        }
    }

    void Update()
    {
        if(dataContainer.killBeast)
        {
            dontAppear = true;
            if (beast.activated) beast.State.StopHunting();
            dataContainer.killBeast = false;
            noSpawning = true;
        }

        if (noSpawning)
        {
            waitCoutnerAfterCheatKill += Time.deltaTime;
            if (waitCoutnerAfterCheatKill >= 30f)
            {
                noSpawning = false;
                waitCoutnerAfterCheatKill = 0f;
            }
        }
        closestChemin = dataContainer.currentLevelChemins.OrderBy(chemin => Vector3.Distance(dataContainer.playerPos, chemin.transform.position)).ToList<CheminBeast>();
        #region Data Container Updates
        searchStarted = dataContainer.searchStarted;
        inLOS = dataContainer.inLOS;
        dataContainer.menaceGauge = System.Math.Round(Mathf.Clamp((dataContainer.framesSinceSearch + dataContainer.framesInLOS) / Mathf.Pow((dataContainer.searchMax + dataContainer.losMax), amortissement), 0f, 1f), 2);
        menaceGauge = dataContainer.menaceGauge;
        #endregion

        if (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame && Gamepad.current.rightShoulder.wasPressedThisFrame)
        {
            if (beast.activated) beast.State.StopHunting();
            dataContainer.zoneNotFound = true;
        }

    }

    private void FixedUpdate()
    {
        dataContainer.beastAngerLevel = beastAngerLevel;

        


        if (startChecks)
        {
            if (player != null)
            {
                playerZone = player.currentZone;
                playerPos = player.gameObject.transform.position;
                
                dataContainer.playerPos = playerPos;
            }
        }

        if (!dontAppear)
        {
            absYOffset = Mathf.Abs(playerPos.y - beast.tr.position.y);
            dataContainer.absYOffset = absYOffset;

            #region Beast Error Handling
            // If player has no current Chemin
            if (dataContainer.zoneNotFound)
            {
                timeSinceTooFar += Time.deltaTime;
                if (timeSinceTooFar >= 25f)
                {
                    timeSinceTooFar = 0f;
                    dataContainer.zoneNotFound = false;
                }
            }
            #endregion

            #region Player Calculations
            if (startChecks)
            {
                if (player != null)
                {
                    playerZone = player.currentZone;
                    playerPos = player.gameObject.transform.position;
                    dataContainer.playerPos = playerPos;
                    playerZone = player.currentZone;

                    if (!dontAppear && beast.activated)
                    {
                        // Calcule la distance entre le joueur et l'IA, et envoie l'IA sur le chemin du joueur si elle est trop grande
                        playerAIDistance = Vector3.Distance(playerPos, beast.transform.position);
                        aiTooFar = playerAIDistance >= 30f;
                        
                        if (aiTooFar) beast.tr.position = closestChemin[0].waypoints[0].position;
                        else if (!dataContainer.zoneNotFound && dataContainer.player.currentZone == null) dataContainer.zoneNotFound = true;
                    }
                }
            #endregion
        
            // Mise a jour des donnees relatives au joueur


            #region Menace Gauge grow/decrease logic
            UpdateCounters(dataContainer.searchStarted, dataContainer.inLOS);
            #endregion

            #region Overflow prevention (positive or negative)
            // Min tout le monde (evite overflow negatif)
            if (dataContainer.menaceGauge == 0f && (!dataContainer.searchStarted && !dataContainer.inLOS))
            {
                dataContainer.framesInLOS = 0f;
                dataContainer.framesSinceSearch = 0f;
            }

            // Max tout le monde (evite overflow positif)
            else if (dataContainer.menaceGauge == 1f && (dataContainer.searchStarted && dataContainer.inLOS))
            {
                dataContainer.framesInLOS = dataContainer.losMax;
                dataContainer.framesSinceSearch = dataContainer.searchMax;
            }
                #endregion
            }

        }

        if (!overrideMG && !dontAppear) // Menace Gauge Mode
        {
            // Check if MG is too low
            if (beast != null && menaceGauge < 0.2f && !beast.idle.wait && closestChemin.Count > 0 && !noSpawning) beast.State.StartHunting(closestChemin[0]);
            else if (beast != null && beast.idle.wait && !waitForSpawn) waitForSpawn = true;

            if ((menaceGauge > 0.8f && beast.State != beast.chase) || noSpawning) 
            { 
                beast.State.StopHunting();
                beastAngerLevel = 0f;
            }
        }
        else if (overrideMG && !dontAppear)
        {
            if (beast != null && !beast.idle.wait && closestChemin.Count > 0 && !noSpawning) beast.State.StartHunting(closestChemin[0]);
            else if (beast != null && beast.idle.wait && !waitForSpawn) waitForSpawn = true;
        }
        else
        {
            if(beast != null && beast.activated) beast.State.StopHunting();
        }

        if (waitForSpawn && waitSpawn == null) waitSpawn = StartCoroutine(WaitBeforeSpawn(10f));
    }
    #endregion

    #region Custom
    private void UpdateCounters(params bool[] factors)
    {
        if (factors[0]) dataContainer.framesSinceSearch = Mathf.Clamp(dataContainer.framesSinceSearch + 1, 0f, Mathf.Pow(300f, amortissement));
        else dataContainer.framesSinceSearch = Mathf.Clamp(dataContainer.framesSinceSearch - 1, 0f, Mathf.Pow(300f, amortissement));

        if (factors[1]) dataContainer.framesInLOS = Mathf.Clamp(dataContainer.framesInLOS + 1, 0f, Mathf.Pow(150f, amortissement));
        else dataContainer.framesInLOS = Mathf.Clamp(dataContainer.framesInLOS - 1, 0f, Mathf.Pow(150f, amortissement));
    }
    #endregion

    #endregion

    IEnumerator WaitBeforeSpawn(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        beast.SetState(beast.idle);
        waitForSpawn = false;
        waitSpawn = null;
        yield break;
    }

    private void GetPlayer()
    {
        player = dataContainer.player;
        if (player != null)
        {
            playerPos = player.gameObject.transform.position;
            dataContainer.playerPos = playerPos;
        }
        startChecks = true;
    }

    private void OnEnable()
    {
        playerPos = Vector3.zero;
    }

    public void DontAppear()
    {
        dontAppear = true;
    }

    public void CanAppearAgain()
    {
        dontAppear = false;
    }

    public void CheckCurrentLevel()
    {
        closestChemin.Clear();
        dataContainer.currentLevelChemins.Clear();
        beast.SetState(beast.idle);
        bool result = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name.Contains("strap")) continue;
            if (!SceneManager.GetSceneAt(i).name.Contains("HUB") || !SceneManager.GetSceneAt(i).name.Contains("LVL")) continue;

            result = true;
        }
        dontAppear = result;
    }
}

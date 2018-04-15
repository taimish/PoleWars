using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class PoleControl : MonoBehaviour {
    /// PUBLIC **************************************************************************
    // LINKS
    public MainGlobalObjects globalObjects;
    public GameObject selfSensor;
    public GameObject poleAssembly;
    public GameObject poleSensor;
    public BeltType[] beltTypes;
    public GameObject[] belts;
    public Image energyBar;
    public Text lifeText;

    // PARAMS
    public PoleParams poleParams;
    public PoleModification poleModif;
    public Vector3 AIMoveDir;
    public Vector3 poleSpeed;
    public Vector3 recoilSpeed;
    public float energy = 1000;
    public bool boostModeOn = false;

    // INFO
    public string[] AIParams;

    // AI ORIENTATION PARAMS

    // ENUMS

    /// PRIVATE **************************************************************************
    // LINKS
    private BarControl energyBarControl;
    private Transform poleAssemblyTransform;
    private BeltControl[] beltControls;
    private GameObject leftEngineFire;
    private GameObject rightEngineFire;
    private GameObject upEngineFire;
    private GameObject downEngineFire;
    private GameObject bodyDamagedLightning;
    private GameObject engineDamagedLightning;
    private GameObject sensorDamagedLightning;
    private AudioSource antigravityEngineSound;

    // PARAMS
    private delegate void BehaviorDelegate();
    private BehaviorDelegate Behave;
    private BehaviorDelegate AIBehave;
    private Vector3[] AICursors;
    private int currDivider = 0;
    private float currEnergyAccelBoost = 1;
    private float currEnergySpeedBoost = 1;
    private float polePositionHeight = 0;
    private float poleModelRelationHeightTop;
    private float poleModelRelationHeight;
    private float poleModelRelationHeightBottom;
    private float sensorSpin;
    private bool lastSeenAlive = false;
    private Vector3 lastSeenCoords;
    private float[] orientAngles = { 45, 22.5F, 0.1F, -1, -22.5F, -45 };
    //private float[] orientAngles = { 90, 45, 22.5F, 0.1F, -1, -22.5F, -45, -90 };




    // START
    void Start ()
    {
        // ENERGY INIT
        energy = poleParams.poleMaxEnergy * poleModif.energyMaxCoef;
        energyBarControl = energyBar.GetComponent<BarControl>();

        // SPEED INIT
        poleSpeed = new Vector3(0, 0, 0);

        // GETTING BELT CONTROLS
        Array.Resize(ref beltControls, belts.Length);
        for (int i = 0; i < beltControls.Length; i++)
        {
            beltControls[i] = belts[i].GetComponent<BeltControl>();
        }

        // INFO INIT
        AIParams = new string[6];
        AIParams[0] = "AIRandomMove: " + poleParams.AIRandomMove.ToString();
        AIParams[1] = "AISearchingMove: " + poleParams.AISearchingMove.ToString();
        AIParams[2] = "AIFiringOnContact: " + poleParams.AIFiringOnContact.ToString();
        AIParams[3] = "AITargetMovePrediction: " + poleParams.AITargetMovePrediction.ToString();
        AIParams[4] = "AIManuvering: " + poleParams.AIManuvering.ToString();
        AIParams[5] = "AISeekLastPosition: " + poleParams.AISeekLastPosition.ToString();

        // BEHAVIOR DELEGATE INIT
        switch (poleParams.type)
        {
            case PoleType.Player:
                Behave = new BehaviorDelegate(PlayerBehavior);
                break;
            case PoleType.Enemy:
                AIBehave = new BehaviorDelegate(EnemyBehavior);
                InitializeAI();
                break;
            case PoleType.NeutralStatic:
                AIBehave = new BehaviorDelegate(NeutralStaticBehavior);
                InitializeAI();
                break;
            case PoleType.NeutralMovable:
                AIBehave = new BehaviorDelegate(NeutralStaticBehavior);
                InitializeAI();
                break;
            default:
                break;
        }

        // POLE ASSEMBLY TRANSFORM INIT
        poleAssemblyTransform = poleAssembly.GetComponent<Transform>();

        // POLE HEIGHT LIMIT AND PARAM INIT
        poleModelRelationHeightTop = poleAssemblyTransform.position.y - transform.position.y;
        poleModelRelationHeightBottom = poleModelRelationHeightTop - poleParams.poleModelMaxRelationHeightDiff;

        // EFFECTS INIT
        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            switch (child.gameObject.name)
            {
                case "DownEngineFire":
                    downEngineFire = child.gameObject;
                    break;
                case "UpEngineFire":
                    upEngineFire = child.gameObject;
                    break;
                case "RightEngineFire":
                    rightEngineFire = child.gameObject;
                    break;
                case "LeftEngineFire":
                    leftEngineFire = child.gameObject;
                    break;
                case "BodyDamagedLightning":
                    bodyDamagedLightning = child.gameObject;
                    break;
                case "EngineDamagedLightning":
                    engineDamagedLightning = child.gameObject;
                    break;
                case "SensorDamagedLightning":
                    sensorDamagedLightning = child.gameObject;
                    break;
                default:
                    break;
            }
        }
        var leftEngineFireEmission = leftEngineFire.GetComponent<ParticleSystem>().emission;
        var rightEngineFireEmission = rightEngineFire.GetComponent<ParticleSystem>().emission;
        var upEngineFireEmission = upEngineFire.GetComponent<ParticleSystem>().emission;
        var downEngineFireEmission = downEngineFire.GetComponent<ParticleSystem>().emission;
        leftEngineFireEmission.enabled = false;
        rightEngineFireEmission.enabled = false;
        upEngineFireEmission.enabled = false;
        downEngineFireEmission.enabled = false;

        var bodyDamagedLightningEmission = bodyDamagedLightning.GetComponent<ParticleSystem>().emission;
        var engineDamagedLightningEmission = engineDamagedLightning.GetComponent<ParticleSystem>().emission;
        var sensorDamagedLightningEmission = sensorDamagedLightning.GetComponent<ParticleSystem>().emission;
        bodyDamagedLightningEmission.enabled = false;
        engineDamagedLightningEmission.enabled = false;
        sensorDamagedLightningEmission.enabled = false;

        // SOUND INIT
        antigravityEngineSound = gameObject.GetComponent<AudioSource>();
    }




    // USUAL UPDATE
    void Update ()
    {
        lifeText.text = "B: " + poleParams.hitsToBody.ToString() + "  E: " + poleParams.hitsToEngine.ToString() + "  S: " + poleParams.hitsToSensor.ToString();
        BarsManagement();
        Behave();
        lifeText.text += "\nAIMoveDir: " + AIMoveDir.ToString();
        //lifeText.text += "\nPoleSpeed: " + poleSpeed.ToString();
        StatsManagement();
    }




    // FIXED UPDATE
    void FixedUpdate()
    {
        MovePole();
    }




    // ********************************************************************************************************
    // POLE MOVEMENT METHODS **********************************************************************************
    // ********************************************************************************************************




    // POLE LINEAR SPEED CALCULATION
    public void SetPoleAccel(float xAcceleration = 0, float zAcceleration = 0)
    {
        // CHECKING ACCELERATION BOUNDS
        if (xAcceleration > 1)
            xAcceleration = 1;
        else if (xAcceleration < -1)
            xAcceleration = -1;

        if (zAcceleration > 1)
            zAcceleration = 1;
        else if (zAcceleration < -1)
            zAcceleration = -1;

        float totalAccel = poleParams.poleAcceleration * currEnergyAccelBoost * poleModif.poleAccelerationCoef * (1 - 0.2f * poleParams.hitsToEngine);
        float totalMaxSpeed = poleParams.poleMaxSpeed * currEnergySpeedBoost * poleModif.poleMaxSpeedCoef * (1 - 0.2f * poleParams.hitsToEngine);

        var leftEngineFireEmission = leftEngineFire.GetComponent<ParticleSystem>().emission;
        var rightEngineFireEmission = rightEngineFire.GetComponent<ParticleSystem>().emission;
        var upEngineFireEmission = upEngineFire.GetComponent<ParticleSystem>().emission;
        var downEngineFireEmission = downEngineFire.GetComponent<ParticleSystem>().emission;
        //lifeText.text += "\nAccels (hor, vert): " + xAcceleration.ToString() + "  " + zAcceleration.ToString();

        if (xAcceleration == 0)
        {
            // STOPPING ON X AXIS
            poleSpeed.x *= poleParams.poleSpeedDegradationCoef;
        }
        else
        {
            // ACCELERATING ON X AXIS
            poleSpeed.x += xAcceleration * totalAccel;
        }

        if (zAcceleration == 0)
        {
            // STOPPING ON Z AXIS
            poleSpeed.z *= poleParams.poleSpeedDegradationCoef;
        }
        else
        {
            // ACCELERATING ON Z AXIS
            poleSpeed.z += zAcceleration * totalAccel;
        }

        // SIDE ENGINE FIRE SETTING
        if (xAcceleration == 0)
        {
            leftEngineFireEmission.enabled = false;
            rightEngineFireEmission.enabled = false;
        }
        else if (xAcceleration > 0)
        {
            leftEngineFireEmission.enabled = true;
            rightEngineFireEmission.enabled = false;
        }
        else
        {
            leftEngineFireEmission.enabled = false;
            rightEngineFireEmission.enabled = true;
        }

        if (zAcceleration == 0)
        {
            upEngineFireEmission.enabled = false;
            downEngineFireEmission.enabled = false;
        }
        else if (zAcceleration > 0)
        {
            upEngineFireEmission.enabled = false;
            downEngineFireEmission.enabled = true;
        }
        else
        {
            upEngineFireEmission.enabled = true;
            downEngineFireEmission.enabled = false;
        }

        // SPEED VECTOR MODULE CHECK
        if (poleSpeed.magnitude > poleParams.poleMaxSpeed * poleModif.poleMaxSpeedCoef)
            poleSpeed = poleSpeed / poleSpeed.magnitude * totalMaxSpeed;

        // COUNTING IN RECOIL
        poleSpeed.x += recoilSpeed.x;
        poleSpeed.z += recoilSpeed.z;
        recoilSpeed.x *= 0.9f;
        recoilSpeed.z *= 0.9f;
    }




    // POLE RECOIL SPEED CHANGE
    void RecoilSpeedCorrection(Vector3 projectileRecoil)
    {
        poleSpeed += projectileRecoil;

        // SPEED VECTOR MODULE CHECK
        if (poleSpeed.magnitude > poleParams.poleMaxSpeed * poleModif.poleMaxSpeedCoef * (1 - 0.2f * poleParams.hitsToEngine))
            poleSpeed = poleSpeed / poleSpeed.magnitude * poleParams.poleMaxSpeed * poleModif.poleMaxSpeedCoef;
    }




    // SENSOR SPIN CALCULATION
    public void SetSensorSpin(float spin = 0)
    {
        sensorSpin = spin * poleParams.rotationK * (1 - 0.2f * poleParams.hitsToSensor);
        if (Math.Abs(sensorSpin) > poleParams.sensorMaxSpin)
            sensorSpin = poleParams.sensorMaxSpin * Math.Sign(sensorSpin);
    }




    // POLE ROTATION AND MOVEMENT AND SENSOR ROTATION
    public void MovePole()
    {
        poleModelRelationHeight = poleModelRelationHeightTop + (float)Math.Sin(Time.time * poleParams.poleLevitationPeriod) * poleParams.poleLevitationAmplitude;
        // IF NEED TO STAND
        if (!poleParams.poleFly && (polePositionHeight > poleModelRelationHeightBottom))
        {
            polePositionHeight -= poleParams.poleVerticalSpeed;
            poleAssemblyTransform.Translate(new Vector3(0, -poleParams.poleVerticalSpeed, 0));
        }

        // IF NEED TO FLY
        if (poleParams.poleFly)
        {
            float diffHeight = poleModelRelationHeight - polePositionHeight;
            if (diffHeight >= poleParams.poleVerticalSpeed)
            {
                polePositionHeight += poleParams.poleVerticalSpeed;
                poleAssemblyTransform.Translate(new Vector3(0, poleParams.poleVerticalSpeed, 0));
            }
            else if (diffHeight <= -poleParams.poleVerticalSpeed)
            {
                polePositionHeight -= poleParams.poleVerticalSpeed;
                poleAssemblyTransform.Translate(new Vector3(0, -poleParams.poleVerticalSpeed, 0));
            }

        }

        // IF NOT STANDING - MOVE AND ROTATE
        if (polePositionHeight > poleModelRelationHeightBottom)
        {
            // MOVING
            transform.Translate(poleSpeed);
        }

        // ROTATING SENSOR
        poleSensor.transform.Rotate(new Vector3(0, sensorSpin, 0));
    }




    // ********************************************************************************************************
    // POLE BEHAVIOR METHODS **********************************************************************************
    // ********************************************************************************************************
    



    // PLAYER BEHAVIOR
    void PlayerBehavior()
    {
        // GETTING POLE MOVEMENT
        SetPoleAccel(Math.Sign(Input.GetAxis("Horizontal")), Math.Sign(Input.GetAxis("Vertical")));

        // SETTING BELT TARGETING AT CURSOR POSITION
        for (int i = 0; i < beltControls.Length; i++)
        {
            beltControls[i].targetPoint = globalObjects.mainCursor.transform.position;
        }

        // CALCULATING SENSOR ROTATION ANGLE
        Vector3 sensorForwardDirFlat = poleSensor.transform.forward;
        sensorForwardDirFlat.y = 0;
        Vector3 toCursorFlat = (globalObjects.mainCursor.transform.position - poleSensor.transform.position);
        toCursorFlat.y = 0;
        float diffHorizAngle = Vector3.SignedAngle(sensorForwardDirFlat, toCursorFlat, Vector3.up);
        SetSensorSpin(diffHorizAngle);

        // GETTING FIRE INPUTS
        if (Input.GetKey(KeyCode.Mouse0))
        {
            // LEFT MOUSE BUTTON PRESSED - FIRING LEFT BELTS
            foreach (BeltControl beltControl in beltControls)
            {
                if (beltControl.beltParams.side == BeltSide.left)
                    beltControl.Fire();
            }
        }
        if (Input.GetKey(KeyCode.Mouse1))
        {
            // RIGHT MOUSE BUTTON PRESSED - FIRING RIGHT BELTS
            foreach (BeltControl beltControl in beltControls)
            {
                if (beltControl.beltParams.side == BeltSide.right)
                    beltControl.Fire();
            }
        }
        if (Input.GetKey(KeyCode.Q))
        {
            // Q KEY PRESSED - FIRING FRONT BELTS
            foreach (BeltControl beltControl in beltControls)
            {
                if (beltControl.beltParams.side == BeltSide.front)
                    beltControl.Fire();
            }
        }
        if (Input.GetKey(KeyCode.E))
        {
            // E KEY PRESSED - FIRING BACK BELTS
            foreach (BeltControl beltControl in beltControls)
            {
                if (beltControl.beltParams.side == BeltSide.back)
                    beltControl.Fire();
            }
        }

        // GETTING OTHER INPUTS
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            poleParams.poleFly = !poleParams.poleFly;

            // CONTROLLING UNTIGRAV SOUND
            if (poleParams.poleFly)
            {
                antigravityEngineSound.enabled = true;
            }
            else
            {
                antigravityEngineSound.enabled = false;
            }
        }

        if ((Input.GetKeyDown(KeyCode.LeftShift)) && (energy > poleParams.poleEnergyAccelerationThreshold / 100 * poleParams.poleMaxEnergy * poleModif.energyMaxCoef))
        {
            SetBoostParams(true);
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            SetBoostParams(false);
        }
    }




    // GENERAL UI BEHAVIOR
    void AIBehavior()
    {
        // DIVIDER CHECK FOR OUTPUT OPTIMIZATION
        currDivider++;
        if (currDivider >= poleParams.divider)
        {
            currDivider = 0;
            // THE AI BEHAVIOR
            AIBehave();
        }
    }




    // ENEMY BEHAVIOR
    void EnemyBehavior()
    {
        if ((poleParams.AIRandomMove) && (globalObjects.playerPole != null))
        {
            /// ONLY MOVING
            lifeText.text = "MOVING";
            bool freeMove = true;
            bool engage = false;
            poleParams.poleFly = true;
            // LOOKING AROUND
            float minObstackleDist = 50;
            float closestObstackleAngle = 0.1F;
            float[] currDists = new float[orientAngles.Length];
            for (int i = 0; i < orientAngles.Length; i++)
            {
                // SENDING LOOKING RAYS
                Vector3 tmpDir = Quaternion.AngleAxis(orientAngles[i], Vector3.up) * AIMoveDir;
                Ray tmpRay = new Ray(transform.position + new Vector3(0, 0, 0) + tmpDir * 1, tmpDir);
                RaycastHit tmpInfo;
                currDists[i] = 50;
                if (Physics.Raycast(tmpRay, out tmpInfo, 50))
                    currDists[i] = tmpInfo.distance;
                if (currDists[i] < minObstackleDist)
                {
                    minObstackleDist = tmpInfo.distance;
                    closestObstackleAngle = orientAngles[i];
                }

            }

            // IF THE OBSTAKLE IS NEAR - LETS TURN AWAY, NO FREE MOVE
            if (minObstackleDist < poleParams.minMinObstacleDist)
            {
                lifeText.text += "\nAVOIDING (" + minObstackleDist.ToString() + ")";
                // TURN RIGHT OR LEFT
                //float needSpin = -Math.Sign(closestObstackleAngle);
                float needSpin = -Math.Sign(closestObstackleAngle) * 2;
                AIMoveDir = Quaternion.Euler(0, needSpin, 0) * AIMoveDir;
                Vector3 AIAccelDir = new Vector3(AIMoveDir.x, 0, AIMoveDir.z).normalized;
                SetPoleAccel(AIAccelDir.x, AIAccelDir.z);
                freeMove = false;
            }

            if (poleParams.AISeekLastPosition || poleParams.AIFiringOnContact)
            {
                /// ENGAGING
                // PREPARING VECTORS
                Vector3 targetDir = globalObjects.playerPole.transform.position - transform.position;
                Vector3 targetDirFlat = globalObjects.playerPole.transform.position - transform.position;
                targetDirFlat.y = 0;
                Vector3 forwardFlat = transform.forward;
                forwardFlat.y = 0;

                // TRYING TO SEE PLAYER MECH
                float actualAIViewDistance = poleParams.AIViewDistance * (1 - 0.15f * poleParams.hitsToSensor);
                float actualAIDistanceRange = poleParams.AIDistanceRange * (1 - 0.15f * poleParams.hitsToSensor);

                lifeText.text += "\nSEARCHING";
                float distance = -1;
                Transform tmpTransf = null;
                RaycastHit eLookInfo;
                Vector3 eLookDir = globalObjects.playerPole.transform.position + new Vector3(0, 0, 0) - selfSensor.transform.position;
                Ray eLook = new Ray(selfSensor.transform.position + eLookDir.normalized, eLookDir);
                bool eHit = Physics.Raycast(eLook, out eLookInfo, actualAIViewDistance);
                if (eHit)
                {
                    lifeText.text += "\n PLAYER VIEW RAY HIT: " + eLookInfo.collider.gameObject.name + "   WITH DIST: " + eLookInfo.distance.ToString();
                    // GETTING PARENT OF HITED COLLIDER
                    tmpTransf = eLookInfo.collider.transform;
                    while (tmpTransf.parent != null)
                    {
                        tmpTransf = tmpTransf.parent;
                    }
                }

                if (eHit && (tmpTransf.gameObject == globalObjects.playerPole))
                {
                    lifeText.text += "\nPLAYER DETECTED";
                    distance = eLookInfo.distance;
                    if (distance <= actualAIViewDistance)
                    {
                        engage = true;
                        lastSeenAlive = true;
                        lastSeenCoords = globalObjects.playerPole.transform.position;
                    }
                }

                if (engage)
                {
                    /// ENGAGE!
                    lifeText.text += "\nENGAGING";

                    // CALCULATING SENSOR ROTATION ANGLE
                    Vector3 sensorForwardDirFlat = poleSensor.transform.forward;
                    sensorForwardDirFlat.y = 0;
                    Vector3 toCursorFlat = (globalObjects.playerPole.transform.position - poleSensor.transform.position);
                    toCursorFlat.y = 0;
                    float diffHorizAngle = Vector3.SignedAngle(sensorForwardDirFlat, toCursorFlat, Vector3.up);
                    SetSensorSpin(diffHorizAngle);

                    // PREPARING VECTORS
                    Vector3 playerForwardFlat = globalObjects.playerPole.transform.forward;
                    playerForwardFlat.y = 0;

                    // SETTING BELTS' TARGET POINTS
                    if (poleParams.AITargetMovePrediction)
                    {
                        // SEEKENG TARGET PREDICTED POSITION
                        lifeText.text += "\nTARGETING WITH PREDICTION";
                        /*float angleTDToPV = Vector3.SignedAngle(targetDir, playerPoleAssembly.transform.forward, Vector3.up); // Angle of target direction to player velocity
                        float playerSpeed = playerPoleAssembly.GetComponent<MechControlForces>().trackSpeed;
                        float bulletSpeed = bulletPrefab.GetComponent<BulletControl>().velocity;
                        float angleOfPrediction = Math.Sign(angleTDToPV) * (float)Math.Asin(Math.Sin(angleTDToPV) * playerSpeed / bulletSpeed) * 180 / (float)Math.PI;
                        lifeText.text += "\nBeta= " + angleTDToPV.ToString() + " Ps= " + playerSpeed.ToString();
                        lifeText.text += "\nBs= " + bulletSpeed.ToString() + " aOP= " + angleOfPrediction.ToString();
                        firingDir = Quaternion.AngleAxis(angleOfPrediction, Vector3.up) * targetDir;*/

                        // OR FIRING AT CURRENT TARGET POSITION
                        for (int i = 0; i < belts.Length; i++)
                        {
                            beltControls[i].targetPoint = globalObjects.playerPole.transform.position;
                        }
                    }
                    else
                    {
                        // OR FIRING AT CURRENT TARGET POSITION
                        lifeText.text += "\nTARGETING WITHOUT PREDICTION";
                        for (int i = 0; i < belts.Length; i++)
                        {
                            beltControls[i].targetPoint = globalObjects.playerPole.transform.position;
                        }
                    }

                    if (poleParams.AIFiringOnContact)
                    {
                        /// FIRING
                        lifeText.text += "\nFIRING";
                        foreach (BeltControl beltControl in beltControls)
                        {
                            beltControl.Fire();
                        }

                        // MOVING IN COMBAT
                        if (freeMove)
                        {
                            if (poleParams.AIManuvering)
                            {
                                // MANUVER WHILE FIREING
                                lifeText.text += "\nEVADING";
                                if (distance > actualAIViewDistance * (100 - actualAIDistanceRange * 0.5) / 100)
                                {
                                    // CLOSING TO PLAYER
                                    lifeText.text += "   - TO";
                                    AIMoveDir = Quaternion.Euler(0, UnityEngine.Random.Range(-10, 10), 0) * globalObjects.playerPole.transform.position - transform.position;
                                }
                                else if (distance > actualAIViewDistance * (100 - actualAIDistanceRange * 1.5) / 100)
                                {
                                    // MOVING AROUND PLAYER AT DISTANCE
                                    lifeText.text += "   - AROUND";
                                    if (UnityEngine.Random.Range(0, 10) > 5)
                                        AIMoveDir = (Quaternion.Euler(0, UnityEngine.Random.Range(80, 100), 0) * globalObjects.playerPole.transform.position - transform.position);
                                    else
                                        AIMoveDir = (Quaternion.Euler(0, UnityEngine.Random.Range(260, 280), 0) * globalObjects.playerPole.transform.position - transform.position);
                                }
                                else
                                {
                                    // MOVING FROM PLAYER
                                    lifeText.text += "   - FROM";
                                    AIMoveDir = Quaternion.Euler(0, UnityEngine.Random.Range(170, 190), 0) * globalObjects.playerPole.transform.position - transform.position;
                                }
                                Vector3 AIAccelDir = new Vector3(AIMoveDir.x, 0, AIMoveDir.z).normalized;
                                SetPoleAccel(AIAccelDir.x, AIAccelDir.z);
                            }
                            else
                            {
                                // STOPPING
                                /*if (poleSpeed.magnitude > 0)
                                    SetPoleAccel(0, 0);*/

                                poleParams.poleFly = false;
                            }
                        }
                    }
                }
                else
                {
                    if (poleParams.AISeekLastPosition && lastSeenAlive && freeMove)
                    {
                        // SEEKING LAST SEEN PLAYER POSITION
                        lifeText.text += "\nDISAPPEARED";
                        Vector3 lastSeenDirFlat = lastSeenCoords - transform.position;
                        lastSeenDirFlat.y = 0;
                        if (lastSeenDirFlat.magnitude < 5)
                        {
                            // LAST SEEN PLAYER LOCATION IS REACHED
                            lastSeenAlive = false;
                            freeMove = true;
                        }
                        else
                        {
                            // AIMING AT LAST KNOWN POSITION
                            for (int i = 0; i < belts.Length; i++)
                            {
                                beltControls[i].targetPoint = lastSeenCoords + new Vector3(0, poleModelRelationHeightTop, 0);
                            }

                            // MOVE TO LOCATION
                            lifeText.text += "\nSEEKING";
                            freeMove = false;
                            AIMoveDir = lastSeenDirFlat;
                            Vector3 AIAccelDir = new Vector3(AIMoveDir.x, 0, AIMoveDir.z).normalized;
                            SetPoleAccel(AIAccelDir.x, AIAccelDir.z);
                        }
                    }
                }
            }

            if (!engage && freeMove)
            {
                // FREE MOVING IF NO ENGAGE AND OBSTACLES
                lifeText.text += "\nFREEMOVING";
                // TURNING BELTS FORWARD
                for (int i = 0; i < belts.Length; i++)
                {
                    beltControls[i].targetPoint = (transform.position + AIMoveDir) * 5 + new Vector3(0, poleModelRelationHeightTop, 0);
                }

                float angleToTurn = 0;
                if (UnityEngine.Random.Range(00, 100) < 4)
                {
                    angleToTurn += UnityEngine.Random.Range(0, 15);
                    AIMoveDir = Quaternion.Euler(0, angleToTurn, 0) * AIMoveDir;
                }
                Vector3 AIAccelDir = new Vector3(AIMoveDir.x, 0, AIMoveDir.z).normalized;
                SetPoleAccel(AIAccelDir.x, AIAccelDir.z);
            }
        }
    }




    // NEUTRAL STATIC BEHAVIOR
    void NeutralStaticBehavior()
    {
    }




    // NEUTRAL MOVEABLE BEHAVIOR
    void NeutralMoveableBehavior()
    {
    }




    // ********************************************************************************************************
    // OTHER METHODS ******************************************************************************************
    // ********************************************************************************************************




    // INITIALIZE AI
    public void InitializeAI()
    {
        Behave = new BehaviorDelegate(AIBehavior);
        Array.Resize(ref AICursors, belts.Length);
        /*Array.Resize(ref beltTypes, belts.Length);
        for (int i = 0; i < belts.Length; i++)
        {
            beltTypes[i] = belts[i].GetComponent<BeltControl>().beltParams.type;
        }*/
    }




    // RECIEVING DAMAGE FROM ANY SOURCE
    public void RecieveDamage(string hitPartTag, float projectileDamage)
    {
        float damage = 0;
        switch (hitPartTag)
        {
            case "PoleSensor":
                damage = projectileDamage * 100 / (0.1f + poleParams.resistanceOfSensor);
                if (UnityEngine.Random.Range(0, 99) < damage)
                {
                    // PART HIT
                    if (poleParams.hitsToSensor < 3) poleParams.hitsToSensor++;
                    float emissionRate = poleParams.hitsToSensor * 1f;
                    switch (poleParams.hitsToSensor)
                    {
                        case 1:
                            // LIGHT DAMAGE
                            break;
                        case 2:
                            // HEAVY DAMAGE
                            break;
                        case 3:
                            // CRITICAL DAMAGE
                            break;
                    }
                    var sensorDamagedLightningEmission = sensorDamagedLightning.GetComponent<ParticleSystem>().emission;
                    sensorDamagedLightningEmission.enabled = true;
                    sensorDamagedLightningEmission.rateOverTime = emissionRate;
                }
                break;
            case "PoleEngine":
                damage = projectileDamage * 100 / (0.1f + poleParams.resistanceOfEngine);
                if (UnityEngine.Random.Range(0, 99) < damage)
                {
                    // PART HIT
                    if (poleParams.hitsToEngine < 3) poleParams.hitsToEngine++;
                    float emissionRate = poleParams.hitsToEngine * 0.5f;
                    switch (poleParams.hitsToEngine)
                    {
                        case 1:
                            // LIGHT DAMAGE
                            break;
                        case 2:
                            // HEAVY DAMAGE
                            break;
                        case 3:
                            // CRITICAL DAMAGE
                            Destruction();
                            break;
                    }
                    var engineDamagedLightningEmission = engineDamagedLightning.GetComponent<ParticleSystem>().emission;
                    engineDamagedLightningEmission.enabled = true;
                    engineDamagedLightningEmission.rateOverTime = emissionRate;
                }
                break;
            case "PoleBody":
                damage = projectileDamage * 100 / (0.1f + poleParams.resistanceOfBody);
                if (UnityEngine.Random.Range(0, 99) < damage)
                {
                    // PART HIT
                    if (poleParams.hitsToBody < 3) poleParams.hitsToBody++;
                    float emissionRate = poleParams.hitsToBody * 0.5f;
                    switch (poleParams.hitsToBody)
                    {
                        case 1:
                            // LIGHT DAMAGE
                            break;
                        case 2:
                            // HEAVY DAMAGE
                            break;
                        case 3:
                            // CRITICAL DAMAGE
                            Destruction();
                            break;
                    }
                    var bodyDamagedLightningEmission = bodyDamagedLightning.GetComponent<ParticleSystem>().emission;
                    bodyDamagedLightningEmission.enabled = true;
                    bodyDamagedLightningEmission.rateOverTime = emissionRate;
                }
                break;
            default:
                break;
        }
    }




    // STATS MANAGEMENT
    void StatsManagement()
    {
        // REGEN
        energy += poleParams.poleEnergyRegen * poleModif.energyRegenCoef;
        if (energy > poleParams.poleMaxEnergy * poleModif.energyMaxCoef)
            energy = poleParams.poleMaxEnergy * poleModif.energyMaxCoef;

        // SPENT
        if (boostModeOn)
        {
            energy -= poleParams.poleEnergyAccelerationCost;
        }

        // CHECK ENERGY
        if (energy < 0)
        {
            energy = 0;
            SetBoostParams(false);
        }
    }




    // SHOW ENERGY OF POLE
    public void BarsManagement()
    {
        // ENERGY BAR POSITIONING
        Vector3 newEnergyBarPos = globalObjects.mainCamera.WorldToScreenPoint(poleAssemblyTransform.position) + new Vector3(0, 50, 0);
        energyBar.transform.position = newEnergyBarPos;
        energyBarControl.maxValue = poleParams.poleMaxEnergy * poleModif.energyMaxCoef;
        energyBarControl.currValue = energy;
    }




    // SET BOOST MODE
    void SetBoostParams(bool boostMode)
    {
        boostModeOn = boostMode;
        var leftEngineFireMain = leftEngineFire.GetComponent<ParticleSystem>().main;
        var rightEngineFireMain = rightEngineFire.GetComponent<ParticleSystem>().main;
        var upEngineFireMain = upEngineFire.GetComponent<ParticleSystem>().main;
        var downEngineFireMain = downEngineFire.GetComponent<ParticleSystem>().main;

        if (boostModeOn)
        {
            currEnergySpeedBoost = poleParams.poleEnergyMaxSpeedBoost;
            currEnergyAccelBoost = poleParams.poleEnergyAccelerationBoost;
            leftEngineFireMain.startLifetimeMultiplier = poleParams.poleEnergyAccelerationBoost;
            rightEngineFireMain.startLifetimeMultiplier = poleParams.poleEnergyAccelerationBoost;
            upEngineFireMain.startLifetimeMultiplier = poleParams.poleEnergyAccelerationBoost;
            downEngineFireMain.startLifetimeMultiplier = poleParams.poleEnergyAccelerationBoost;
        }
        else
        {
            currEnergySpeedBoost = 1;
            currEnergyAccelBoost = 1;
            leftEngineFireMain.startLifetimeMultiplier = 1;
            rightEngineFireMain.startLifetimeMultiplier = 1;
            upEngineFireMain.startLifetimeMultiplier = 1;
            downEngineFireMain.startLifetimeMultiplier = 1;
        }
    }




    // DESTRACTION OF POLE
    public void Destruction()
    {
        gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        /*Light tmpLight = gameObject.GetComponentInChildren<Light>();
        if (tmpLight != null)
            Destroy(tmpLight.gameObject);*/
        gameObject.transform.Rotate((float)UnityEngine.Random.Range(-5, 5), 0, (float)UnityEngine.Random.Range(-5, 5));
        poleAssemblyTransform.parent = null;

        energyBar.transform.position = new Vector3(-10000, 10000, 0);
        energyBar.enabled = false;
        Destroy(energyBar);

        List<Transform> childsToEmbody = new List<Transform>();
        List<Transform> childsToDestroy = new List<Transform>();

        // GETTING ALL POLE ASSEMBLY CHILDS INTO TWO GROUPS
        for (int i = 0; i < poleAssemblyTransform.childCount; i++)
        {
            if (poleAssemblyTransform.GetChild(i).tag != "Light")
                childsToEmbody.Add(poleAssemblyTransform.GetChild(i));
            else
                childsToDestroy.Add(poleAssemblyTransform.GetChild(i));
        }

        // GIVING POLE ASSEMBLY CHILD TRANSFORM FREEDOM, RIGIDBODY AND MAKING THEIR COLLIDERS NON-TRIGGER
        foreach (Transform child in childsToEmbody)
        {
            child.parent = null;
            //child.gameObject.AddComponent<Rigidbody>();
            child.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            child.gameObject.GetComponent<Collider>().isTrigger = false;
            BeltControl tmpBeltControl = child.GetComponent<BeltControl>();
            if (tmpBeltControl != null)
                tmpBeltControl.enabled = false;
        }

        // DESTOYING OTHER CHILDREN
        foreach (Transform child in childsToDestroy)
        {
            Destroy(child.gameObject);
        }

        if (poleParams.type == PoleType.Player)
        {
            globalObjects.mainCamera.GetComponent<CameraControl>().focusObject = null;
        }

        gameObject.GetComponent<PoleControl>().enabled = false;
        Destroy(gameObject);
    }
}

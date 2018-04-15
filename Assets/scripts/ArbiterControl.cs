using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ArbiterControl : MonoBehaviour
{
    /// PUBLIC
    // LINKS
    public GameObject mainFocus;
    public Camera mainCamera;
    public Canvas mainCanvas;
    public GameObject mainCursor;
    public Text infoText;

    // PREFABS
    public Image barPrefab;
    public GameObject poleNormPrefab;
    public GameObject sensor1Prefab;

    public GameObject beltLeftPrefab;
    public GameObject beltRightPrefab;
    public GameObject beltFrontPrefab;
    public GameObject beltBackPrefab;

    public GameObject gunRigthPrefub;
    public GameObject gunLeftPrefub;
    public GameObject gunProjectilePrefub;
    public AudioClip gunShotSound;
    public AudioClip gunReloaded;

    public GameObject machinegunRigthPrefub;
    public GameObject machinegunLeftPrefub;
    public GameObject machinegunProjectilePrefub;
    public AudioClip machinegunShotSound;

    public GameObject rocketRigthPrefub;
    public GameObject rocketLeftPrefub;
    public GameObject rocketProjectilePrefub;
    public AudioClip rocketShotSound;

    public GameObject smallClashPrefab;
    public GameObject mediumClashPrefab;
    public GameObject bigClashPrefab;

    // PARAMS
    public GameObject playerPole;
    public int barHeight = 8;
    public List<GameObject> allPoles;

    // TMP PLAYER PARAMS
    public float tmpPoleMaxSpeed = 0;

    /// PRIVATE
    // LINKS

    // PARAMS
    private MainGlobalObjects globalObjects;
    private float playerDeathTime = -1;

    // START --------------------------------------------------------------------------------------------------
    void Start ()
    {
        // INIT OF CLASS WITH MAIN GLOBAL OBJECTS
        globalObjects = new MainGlobalObjects()
        {
            arbiter = gameObject,
            playerPole = null,
            mainFocus = mainFocus,
            mainCamera = mainCamera,
            mainCanvas = mainCanvas,
            mainCursor = mainCursor,
            barPrefab = barPrefab,
            infoText = infoText
        };

        allPoles = new List<GameObject>();
        
        // CREATING PLAYER POLE
        PoleParams playerNormPoleParams = new PoleParams(PoleType.Player);
        if (tmpPoleMaxSpeed != 0)
            playerNormPoleParams.poleMaxSpeed = tmpPoleMaxSpeed;
        PoleModification tmpPoleModif = new PoleModification();
        allPoles.Add(CreateNormPole(allPoles.Count, new Vector3(0, 0, 0), 0, new BeltType[] { BeltType.machinegun, BeltType.gun }, playerNormPoleParams, tmpPoleModif));

        // CREATING ENEMY
        PoleParams enemyNormPoleParams = new PoleParams(PoleType.Enemy,
            true, // newAIRandomMove - moves around randomly changing direction
            false, // newAISearchingMove - moves by waypoints - not currently used
            true, // newAIFiringOnContact - start firing guns when see the player
            false, // newAITargetMovePrediction - predict player motion for more accurate firing - not currently used
            true, // newAIManuvering - manuvering during firing at player (otherwise - standing still)
            true // newAISeekLastPosition - seeks last know position of player when he becomes out of sight
            );
        if (tmpPoleMaxSpeed != 0)
            enemyNormPoleParams.poleMaxSpeed = tmpPoleMaxSpeed;
        allPoles.Add(CreateNormPole(allPoles.Count, new Vector3(50, 0, 0), -60, new BeltType[] { BeltType.gun, BeltType.machinegun }, enemyNormPoleParams, new PoleModification()));
    }

    // UPDATE -------------------------------------------------------------------------------------------------
    void Update ()
    {
        //infoText.text = "";

        if ((playerPole == null) && (playerDeathTime == -1))
            playerDeathTime = Time.time;

        if (((playerDeathTime != -1) && (Time.time - playerDeathTime > 3)) || (Input.GetKey(KeyCode.Escape)))
            Application.Quit();
	}


    // ********************************************************************************************************
    // OBJECTS CREATION ***************************************************************************************
    // ********************************************************************************************************

    
    // CREATION OF NEW NORM POLE
    public GameObject CreateNormPole(int poleID, Vector3 startPoint, float startYRotation, BeltType[] beltTypes, PoleParams poleParams, PoleModification poleModif)
    {
        // CREATE THE NORM POLE
        GameObject newNormPole = Instantiate(poleNormPrefab, startPoint + new Vector3(0, 2.5f, 0), Quaternion.Euler(0, 0, 0));
        PoleControl newNormPoleControl = newNormPole.GetComponent<PoleControl>();
        Vector3 newNormPoleScale = newNormPole.transform.lossyScale;
        Vector3 newNormPoleScaleInv = new Vector3(1 / newNormPoleScale.x, 1 / newNormPoleScale.y, 1 / newNormPoleScale.z);
        Vector3 newNormPoleAIMoveDir = Quaternion.Euler(0, startYRotation, 0) * newNormPole.transform.forward;

        // TRANSFERRING  GLOBAL OBJECTS, POLE PARAMS, MODIFICATION AND ASSEMBLY TO NEW POLE
        newNormPoleControl.globalObjects = globalObjects;
        newNormPoleControl.poleParams = poleParams;
        newNormPoleControl.poleModif = poleModif;
        newNormPoleControl.poleAssembly = newNormPole.transform.GetChild(0).gameObject;
        newNormPoleControl.AIMoveDir = newNormPoleAIMoveDir;

        // IF NEW POLE IS PLAYER - UPDATING GLOBAL PARAMS
        if (poleParams.type == PoleType.Player)
        {
            globalObjects.playerPole = newNormPole;
            playerPole = newNormPole;
            mainCamera.GetComponent<CameraControl>().focusObject = newNormPole;
        }

        // SENSOR CREATION
        GameObject newSensor = Instantiate(sensor1Prefab, startPoint + new Vector3(0, 4.5f, 0), newNormPole.transform.rotation, newNormPoleControl.poleAssembly.transform);
        newNormPoleControl.selfSensor = newSensor;
        newSensor.transform.localScale = newNormPoleScaleInv;

        // CREATING TEXT
        Text newText = Instantiate<Text>(infoText);
        newText.transform.SetParent(mainCanvas.transform);
        newText.transform.position = mainCanvas.transform.TransformPoint( (poleID + 1) * 200 - Screen.width / 2 + 10, Screen.height / 2 - 10, 0);

        // CREATING ENERGYBAR
        Image newEnergyBar = Instantiate<Image>(globalObjects.barPrefab);
        newEnergyBar.transform.SetParent(mainCanvas.transform);
        BarControl newEnergyBarControl = newEnergyBar.GetComponent<BarControl>();
        newEnergyBarControl.maxValue = poleParams.poleMaxEnergy * poleModif.energyMaxCoef;
        newEnergyBarControl.currValue = poleParams.poleMaxEnergy * poleModif.energyMaxCoef;
        newEnergyBarControl.transparency = poleParams.barTransparency;
        newEnergyBarControl.barHeight = barHeight;

        // DEFINING COLORS AND TAG
        switch (poleParams.type)
        {
            case PoleType.Player:
                newEnergyBarControl.backGroundCol = new Color32(0, 10, 73, newEnergyBarControl.transparency);
                newEnergyBarControl.foreGroundCol = new Color32(70, 100, 230, newEnergyBarControl.transparency);
                newText.color = new Color32(0, 230, 10, 255);
                newNormPole.tag = "PlayerTeam";
                break;
            case PoleType.Enemy:
                newEnergyBarControl.backGroundCol = new Color32(0, 10, 73, newEnergyBarControl.transparency);
                newEnergyBarControl.foreGroundCol = new Color32(70, 100, 230, newEnergyBarControl.transparency);
                newText.color = new Color32(225, 0, 0, 255);
                newNormPole.tag = "EnemyTeam";
                break;
            case PoleType.NeutralStatic:
                newText.color = new Color32(160, 160, 160, 255);
                newNormPole.tag = "NeutralTeam";
                break;
            case PoleType.NeutralMovable:
                newText.color = new Color32(200, 200, 200, 255);
                newNormPole.tag = "NeutralTeam";
                break;
        }

        // CREATING BELTS
        if (beltTypes.Length > 4)
            Array.Resize(ref beltTypes, 4);

        GameObject[] belts = new GameObject[beltTypes.Length];
        if (beltTypes.Length > 0)
        {
            // INITIALIZING FIRST BELT PARAMS WITH UNIT PARAMS
            BeltParams[] newBeltsParams = new BeltParams[beltTypes.Length];

            for (int i = 0; i < beltTypes.Length; i++)
            {
                // DEFINITION OF BELT SIDE
                if (UnitGroups.frontUnits.Contains(beltTypes[i]))
                {
                    // FRONT-ORIENTED BELT
                    newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.front, poleID);
                }
                else if (UnitGroups.backUnits.Contains(beltTypes[i]))
                {
                    // BACK-ORIENTED BELT
                    newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.back, poleID);
                }
                else
                {
                    // SIDE-ORIENTED BELT
                    if ((i == 0) || (!UnitGroups.sideUnits.Contains(beltTypes[i - 1])))
                    {
                        // RANDOM SIDE DEFINITION
                        if (UnityEngine.Random.Range(0, 100) < 50)
                        {
                            // BELT IS RANDOMLY LEFT
                            newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.left, poleID);
                        }
                        else
                        {
                            // BELT IS RANDOMLY RIGHT
                            newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.right, poleID);
                        }
                    }
                    else
                    {
                        // DEFINING OTHER DIRECTION THEN IN PREVIOUS BELT
                        if (newBeltsParams[i-1].side == BeltSide.left)
                        {
                            newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.right, poleID);
                        }
                        else
                        {
                            newBeltsParams[i] = CreateBeltParams(beltTypes[i], BeltSide.left, poleID);
                        }
                    }
                }

                // CREATING BELT OBJECT
                belts[i] = CreateBeltObject(poleID, newBeltsParams[i], ref poleModif, newNormPole, 0.55f - i * 0.1f);
            }
        }

        // TRANSFERRING BELTS, SENSOR, TEXT AND HEALTHBAR TO THE NEW POLE OBJECT
        if (beltTypes.Length > 0)
        {
            Array.Resize(ref newNormPoleControl.belts, belts.Length);
            Array.Resize(ref newNormPoleControl.beltTypes, beltTypes.Length);
            belts.CopyTo(newNormPoleControl.belts, 0);
            beltTypes.CopyTo(newNormPoleControl.beltTypes, 0);
        }
        newNormPoleControl.poleSensor = newSensor;
        newNormPoleControl.lifeText = newText;
        newNormPoleControl.energyBar = newEnergyBar;

        return newNormPole;
    }




    // CREATION OF NEW BELT OBJECT
    private GameObject CreateBeltObject(int parentPoleID, BeltParams beltParams, ref PoleModification poleModif, GameObject parentPole, float localHeight)
    {
        // PREPARING OBJECTS AND PREFABS FOR CREATION
        GameObject beltPrefab = null;
        GameObject unitPrefab = null;
        GameObject projectilePrefab = null;
        GameObject newBelt;
        GameObject newUnit;
        Transform parentPoleAssemblyTransf = null;

        // INIT OF PARENT POLE ASSEMBLY TRANSFORM AND INV SCALE
        foreach (Transform transf in parentPole.GetComponentInChildren<Transform>())
        {
            if (transf.tag == "PoleAssembly")
                parentPoleAssemblyTransf = transf;
        }
        Vector3 poleAssamblyScaleInv = new Vector3(1 / parentPoleAssemblyTransf.lossyScale.x, 1 / parentPoleAssemblyTransf.lossyScale.y, 1 / parentPoleAssemblyTransf.lossyScale.z);

        // PREPARING UNIT PARAMS AND CONNECTING THEM TO BELT PARAMS
        UnitParams newUnitParams = CreateUnitParams(beltParams.type, parentPoleID);
        beltParams.unitParams = newUnitParams;

        // PREPARING TMP PARAMS FOR CREATION
        Vector3 newBeltGlobalPos = parentPoleAssemblyTransf.TransformPoint(new Vector3(0, localHeight, 0));
        Vector3 newUnitGlobalPos;
        Vector3 newUnitLocalPosShift = new Vector3(0, 0, 0);
        Quaternion unitRotation = parentPoleAssemblyTransf.rotation;

        // CHOOSING UNIT AND PROJECTILE PREFAB BASED ON BELT TYPE
        switch (beltParams.type)
        {
            case BeltType.gun:
                if (beltParams.side == BeltSide.left)
                    unitPrefab = gunLeftPrefub;
                else
                    unitPrefab = gunRigthPrefub;

                projectilePrefab = gunProjectilePrefub;
                break;
            case BeltType.machinegun:
                if (beltParams.side == BeltSide.left)
                    unitPrefab = machinegunLeftPrefub;
                else
                    unitPrefab = machinegunRigthPrefub;

                projectilePrefab = machinegunProjectilePrefub;
                break;
            case BeltType.rocket:
                if (beltParams.side == BeltSide.left)
                    unitPrefab = rocketLeftPrefub;
                else
                    unitPrefab = rocketRigthPrefub;

                projectilePrefab = rocketProjectilePrefub;
                break;
            case BeltType.plasma:
                break;
            case BeltType.grenadelauncher:
                break;
            case BeltType.railgun:
                break;
            case BeltType.missile:
                break;
            case BeltType.autotracking_lasers:
                break;
            case BeltType.shield:
                break;
            case BeltType.radar:
                break;
            default:
                break;
        }

        // CHOOSING BELT PREFAB, UNIT POSITION SHIFT AND BELT ROTATION AFTER CREATION
        switch (beltParams.side)
        {
            case BeltSide.left:
                beltPrefab = beltLeftPrefab;
                newUnitLocalPosShift = new Vector3(-0.79f, 0, 0);
                break;
            case BeltSide.right:
                beltPrefab = beltRightPrefab;
                newUnitLocalPosShift = new Vector3(0.79f, 0, 0);
                break;
            case BeltSide.front:
                beltPrefab = beltFrontPrefab;
                newUnitLocalPosShift = new Vector3(0, 0, 0.79f);
                break;
            case BeltSide.back:
                beltPrefab = beltBackPrefab;
                newUnitLocalPosShift = new Vector3(0, 0, -0.79f);
                break;
        }


        // CREATION OF NEW BELT GAMEOBJECT
        newBelt = Instantiate(beltPrefab, newBeltGlobalPos, parentPoleAssemblyTransf.rotation, parentPoleAssemblyTransf);
        newBelt.transform.localScale = poleAssamblyScaleInv;
        Vector3 newBeltScaleInv = new Vector3(1 / newBelt.transform.lossyScale.x, 1 / newBelt.transform.lossyScale.y, 1 / newBelt.transform.lossyScale.z);
        BeltControl newBeltControl = newBelt.GetComponent<BeltControl>();

        // CREATION OF NEW UNIT GAMEOBJECT FOR CURRENT BELT
        newUnitGlobalPos = newBelt.transform.TransformPoint(newUnitLocalPosShift);
        newUnit = Instantiate(unitPrefab, newUnitGlobalPos, unitRotation, newBelt.transform);
        newUnit.transform.localScale = newBeltScaleInv;

        //UnitControl newUnitControl = newUnit.GetComponent<UnitControl>();

        // TRANSFERRING BELT SCRIPT PARAMETERS
        newBeltControl.globalObjects = globalObjects;
        newBeltControl.parentPole = parentPole;
        newBeltControl.thisBeltUnit = newUnit;
        newBeltControl.projectilePrefab = projectilePrefab;
        newBeltControl.poleModif = poleModif;
        newBeltControl.beltParams = beltParams;
        Transform[] tmpChildTransfs = newUnit.GetComponentsInChildren<Transform>();
        foreach (Transform child in tmpChildTransfs)
        {
            if (child.tag == "UnitDir")
            {
                newBeltControl.unitDirTransf = child;
                //newUnitControl.unitDirTransf = child;
            }
        }

        //newUnitControl.unitParams = newBeltControl.beltParams.unitParams;

        return newBelt;
    }


    // ********************************************************************************************************
    // PARAMS CREATION ****************************************************************************************
    // ********************************************************************************************************


    // CREATION OF BELT PARAMS
    private BeltParams CreateBeltParams(BeltType beltType, BeltSide beltSide, int parentPoleID)
    {
        BeltParams beltParams = null;
        switch (beltType)
        {
            case BeltType.gun:
                beltParams = new BeltParams()
                {
                    beltMaxSpin = 1.5f,
                    beltMinSpin = 0.1F,
                    beltSpinPrecision = 1f,
                    unitMaxSpin = 1.5f,
                    unitSpinPrecision = 1f,
                    unitMinAngle = -30,
                    unitMaxAngle = 30,
                };
                break;
            case BeltType.machinegun:
                beltParams = new BeltParams()
                {
                    beltMaxSpin = 2f,
                    beltMinSpin = 0.1F,
                    beltSpinPrecision = 0.1f,
                    unitMaxSpin = 2f,
                    unitSpinPrecision = 0.1f,
                    unitMinAngle = -45,
                    unitMaxAngle = 45,
                };
                break;
            case BeltType.rocket:
                beltParams = new BeltParams()
                {
                    beltMaxSpin = 1f,
                    beltMinSpin = 0.1F,
                    beltSpinPrecision = 3f,
                    unitMaxSpin = 1f,
                    unitSpinPrecision = 3f,
                    unitMinAngle = -20,
                    unitMaxAngle = 20,
                };
                break;
            case BeltType.plasma:
                break;
            case BeltType.grenadelauncher:
                break;
            case BeltType.railgun:
                break;
            case BeltType.missile:
                break;
            case BeltType.autotracking_lasers:
                break;
            case BeltType.shield:
                break;
            case BeltType.radar:
                break;
            default:
                break;
        }

        beltParams.type = beltType;
        beltParams.side = beltSide;
        return beltParams;
    }


    // CREATION OF UNIT PARAMS
    private UnitParams CreateUnitParams(BeltType beltType, int parentPoleID)
    {
        UnitParams unitParams = null;
        switch (beltType)
        {
            case BeltType.gun:
                unitParams = new UnitParams()
                {
                    clashPrefab = mediumClashPrefab,
                    unitSound = gunShotSound,
                    unitReloaded = gunReloaded,
                    unitSoundVolume = 0.8f,
                    firePeriod = 2f,
                    fireSpreadAngle = 1f,
                    projectileSpeed = 3f,
                    projectileAccelleration = 0,
                    projectileDamageProbability = 90,
                    explosionDamage = 0,
                    explosionRadius = 0,
                    explosionLightRadius = 1,
                    explosionLightIntensity = 10,
                    explosionLightDuration = 0.5f,
                    travelTime = 10,
                    travelDistance = 200,
                    energyConsumptionPerProjectile = 0,
                    energyConsumptionPerSecond = 0,
                    recoil = 0.05f
                };
                break;
            case BeltType.machinegun:
                unitParams = new UnitParams()
                {
                    clashPrefab = smallClashPrefab,
                    unitSound = machinegunShotSound,
                    unitSoundVolume = 0.3f,
                    firePeriod = 0.3f,
                    fireSpreadAngle = 2,
                    projectileSpeed = 2f,
                    projectileAccelleration = 0,
                    projectileDamageProbability = 10,
                    explosionDamage = 0,
                    explosionRadius = 0,
                    explosionLightRadius = 0.5f,
                    explosionLightIntensity = 6,
                    explosionLightDuration = 0.3f,
                    travelTime = 5,
                    travelDistance = 100,
                    energyConsumptionPerProjectile = 0,
                    energyConsumptionPerSecond = 0,
                    recoil = 0.007f
                };
                break;
            case BeltType.rocket:
                unitParams = new UnitParams()
                {
                    clashPrefab = mediumClashPrefab,
                    unitSound = rocketShotSound,
                    unitSoundVolume = 0.8f,
                    firePeriod = 1.5f,
                    projectileSpeed = 2,
                    projectileMaxSpeed = 5,
                    projectileAccelleration = 0.05f,
                    projectileDamageProbability = 150,
                    explosionDamage = 0,
                    explosionRadius = 0,
                    explosionLightRadius = 10,
                    explosionLightIntensity = 50,
                    explosionLightDuration = 1.5f,
                    travelTime = 5,
                    travelDistance = 100,
                    energyConsumptionPerProjectile = 0,
                    energyConsumptionPerSecond = 0,
                    recoil = 0.05f
                };
                break;
            case BeltType.plasma:
                break;
            case BeltType.grenadelauncher:
                break;
            case BeltType.railgun:
                break;
            case BeltType.missile:
                break;
            case BeltType.autotracking_lasers:
                break;
            case BeltType.shield:
                break;
            case BeltType.radar:
                break;
            default:
                break;
        }

        unitParams.parentPoleID = parentPoleID;
        return unitParams;
    }
}


// ********************************************************************************************************
// ENUMS AND CLASES ***************************************************************************************
// ********************************************************************************************************


// POLE TYPE ENUM - PLAYER OR AI OF THREE TYPES
public enum PoleType
{
    Player,
    Enemy,
    NeutralStatic,
    NeutralMovable
}


// BELT TYPES BY UNIT TYPE
public enum BeltType
{
    gun,
    machinegun,
    rocket,
    plasma,
    grenadelauncher,
    railgun,
    missile,
    autotracking_lasers,
    shield,
    radar
}


// BELT SIDE
public enum BeltSide
{
    left,
    right,
    front,
    back
}


// SENSOR TYPE
public enum SensorType
{
    normal,
    extended,
    visor,
    extended_visor
}


// POLE PART TYPE
public enum PolePart
{
    body,
    engine,
    belt,
    sensor
}


// UNIT GROUPS CLASS
public static class UnitGroups
{
    // GROUPING BY DIRECTION
    public static List<BeltType> sideUnits = new List<BeltType> { BeltType.gun, BeltType.machinegun, BeltType.rocket, BeltType.plasma, BeltType.grenadelauncher, BeltType.railgun };
    public static List<BeltType> frontUnits = new List<BeltType> { BeltType.autotracking_lasers, BeltType.shield };
    public static List<BeltType> backUnits = new List<BeltType> { BeltType.missile, BeltType.radar };

    // GROUPING BY FIRE ABILITY
    public static List<BeltType> fireableUnits = new List<BeltType> { BeltType.gun, BeltType.machinegun, BeltType.rocket, BeltType.plasma, BeltType.grenadelauncher, BeltType.railgun, BeltType.autotracking_lasers, BeltType.missile };
    public static List<BeltType> utilityUnits = new List<BeltType> { BeltType.shield, BeltType.radar };
}


// GLOBAL OBJECTS CLASS
public class MainGlobalObjects
{
    public GameObject arbiter;
    public GameObject playerPole;
    public GameObject mainFocus;
    public Camera mainCamera;
    public Canvas mainCanvas;
    public GameObject mainCursor;
    public Image barPrefab;
    public Text infoText;
}


// PARAMS OF POLE CLASS
public class PoleParams
{
    public int ID = -1;
    public PoleType type = PoleType.NeutralStatic;
    public bool poleFly = true;
    public float poleMaxHealth = 1000;
    public float poleHealthRegen = 0.1f;
    public float poleMaxEnergy = 1000;
    public float poleEnergyRegen = 2;
    public float linearK = 0.005f;
    public float rotationK = 1;
    public float poleMaxSpeed = 0.07F;
    public float poleEnergyMaxSpeedBoost = 1.5f;
    public float poleAcceleration = 0.02f;
    public float poleSpeedDegradationCoef = 0.98f; // 1 - no autostopping, 0 - instant stop
    public float poleEnergyAccelerationBoost = 1.5f;
    public float poleEnergyAccelerationThreshold = 20; // IN PERCENTS
    public float poleEnergyAccelerationCost = 5;
    public float poleMaxSpin = 5;
    public float poleVerticalSpeed = 0.02f;
    public float poleModelMaxRelationHeightDiff = 0.4f;
    public float poleLevitationPeriod = 2f;
    public float poleLevitationAmplitude = 0.2f;
    public float sensorMaxSpin = 2;
    public float AIViewDistance = 25;
    public float AIDistanceRange = 20; // IN PERCENT
    public float minMinObstacleDist = 3;
    public byte barTransparency = 200;
    public int divider = 1;

    // AI COMBAT PARAMS
    public bool AIRandomMove = false;
    public bool AISearchingMove = false;
    public bool AIFiringOnContact = false;
    public bool AITargetMovePrediction = false;
    public bool AIManuvering = false;
    public bool AISeekLastPosition = false;

    // DAMAGE PARAMS
    public byte hitsToBody = 0;
    public byte hitsToEngine = 0;
    public byte hitsToSensor = 0;
    public int resistanceOfBody = 120;
    public int resistanceOfEngine = 50;
    public int resistanceOfSensor = 75;

    // CONSTRUCTORS
    public PoleParams()
    {
    }

    public PoleParams(PoleType poleType)
    {
        type = poleType;
    }

    public PoleParams(PoleType poleType, bool newAIRandomMove, bool newAISearchingMove, bool newAIFiringOnContact, bool newAITargetMovePrediction, bool newAIManuvering, bool newAISeekLastPosition)
    {
        type = poleType;
        AIRandomMove = newAIRandomMove;
        AISearchingMove = newAISearchingMove;
        AIFiringOnContact = newAIFiringOnContact;
        AITargetMovePrediction = newAITargetMovePrediction;
        AIManuvering = newAIManuvering;
        AISeekLastPosition = newAISeekLastPosition;
    }
}


// BELT PARAMS CLASS
public class BeltParams
{
    public BeltSide side;
    public BeltType type;
    public float beltMaxSpin = 1.5f;
    public float beltMinSpin = 0.1F;
    public float beltSpinPrecision = 0.1f;
    public float unitMaxSpin = 1.5f;
    public float unitSpinPrecision = 0.1f;
    public float unitMinAngle = -30;
    public float unitMaxAngle = 30;
    public byte hits = 0;
    public int resistance = 100;
    public UnitParams unitParams;
}


// UNIT PARAMS CLASS
public class UnitParams
{
    public GameObject clashPrefab;
    public AudioClip clashSound;
    public AudioClip unitSound;
    public AudioClip unitReloaded;
    public float unitSoundVolume;
    public int parentPoleID;
    public float firePeriod;
    public float fireSpreadAngle;
    public float projectileSpeed;
    public float projectileMaxSpeed;
    public float projectileAccelleration;
    public float projectileDamageProbability;
    public float explosionDamage;
    public float explosionRadius;
    public float explosionLightRadius;
    public float explosionLightIntensity;
    public float explosionLightDuration;
    public float travelTime;
    public float travelDistance;
    public float energyConsumptionPerProjectile;
    public float energyConsumptionPerSecond;
    public float recoil;

    public UnitParams()
    {
    }

    public UnitParams(int poleID)
    {
        parentPoleID = poleID;
    }
}


/*// WEAPON FIRING CLASS
public static class UnitFiring
{
    public static GunFiring(Vector3 gunDir, )
}*/


// POLE MODIFICATION PARAMS CLASS
public class PoleModification
{
    // POLE COEFS
    public float poleMaxSpeedCoef = 1;
    public float PoleMaxSpinCoef = 1;
    public float poleAccelerationCoef = 1;
    public float healthMaxCoef = 1;
    public float healthRegenCoef = 1;
    public float energyMaxCoef = 1;
    public float energyRegenCoef = 1;
    public float damageReductionCoef = 1;
    public float energyRegenerationCoef = 1;
    // BELT COEFS
    public float beltMaxSpinCoef = 1;
    public float precisionCoef = 1;
    public float unitSpinCoef = 1;
    public float unitMinAngleCoef = 1;
    public float unitMaxAngleCoef = 1;
    // PROJECTILE COEFS
    public float projectileDamageCoef = 1;
    public float explosionDamageCoef = 1;
    public float explosionRadiusCoef = 1;
    public float unitFireRateCoef = 1;
    public float projectileSpeedCoef = 1;
    public float projectileMaxSpeedCoef = 1;
    public float projectileTravelTileCoef = 1;
    public float projectileTravelTimeCoef = 1;
    public float projectileEnergyConsumptionCoef = 1;
    public float projectileRecoilCoef = 1;
}



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BeltControl : MonoBehaviour
{
    /// PUBLIC
    // LINKS
    public MainGlobalObjects globalObjects;
    public GameObject parentPole;
    public GameObject thisBeltUnit;
    public GameObject projectilePrefab;
    public Transform unitDirTransf;
    public PoleModification poleModif;

    // PARAMS
    public BeltParams beltParams;
    public Vector3 targetPoint;

    // FOR INFO
    public string type;
    public string side;

    /// PRIVATE
    // LINKS
    private GameObject beltDamagedLightning;
    private AudioSource unitSound;

    // PARAMS
    private float beltSpin = 0;
    private float unitSpin = 0;
    private float lastShotTime = -100;
    bool unitReloaded = true;
    bool unitReloadSounded = true;

    // Use this for initialization
    void Start ()
    {
        type = beltParams.type.ToString();
        side = beltParams.side.ToString();
        
        // EFFECTS INIT
        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            switch (child.gameObject.name)
            {
                case "BeltDamagedLightning":
                    beltDamagedLightning = child.gameObject;
                    break;
                default:
                    break;
            }
        }

        var beltDamagedLightningEmission = beltDamagedLightning.GetComponent<ParticleSystem>().emission;
        beltDamagedLightningEmission.enabled = false;

        // SOUND INIT
        unitSound = GetComponent<AudioSource>();
    }

    // UPDATE
    void Update ()
    {
        SetBeltAndUnitSpin();

        // RELOADING
        if (!unitReloaded)
        {
            // CHECKING THE RELOAD TIME
            if (Time.time - lastShotTime > beltParams.unitParams.firePeriod / poleModif.unitFireRateCoef / (1 - 0.2f * beltParams.hits))
            {
                unitReloaded = true;
            }

            // CHECKING THE RELOAD SOUND TIME
            if ((beltParams.unitParams.unitReloaded != null) && !unitReloadSounded)
            {
                if (Time.time - lastShotTime > beltParams.unitParams.firePeriod / poleModif.unitFireRateCoef / (1 - 0.2f * beltParams.hits) - 0.5f)
                {
                    // MAKING SOUND
                    unitSound.PlayOneShot(beltParams.unitParams.unitReloaded, beltParams.unitParams.unitSoundVolume);
                    unitReloadSounded = true;
                }
            }


        }


    }


    // FIXED UPDATE
    void FixedUpdate()
    {
        TurnBeltAndUnit();
    }

    // ********************************************************************************************************
    // PLAYER BELT METHODS ************************************************************************************
    // ********************************************************************************************************

    // TORSO SPIN CALCULATION
    public void SetBeltAndUnitSpin()
    {
        // GUNDIR AND TOCURSOR VECTORS ANGLE DIFFERENCE CALCILATION
        Vector3 currUnitForwardDir = thisBeltUnit.transform.forward;
        Vector3 toCursor = (targetPoint - thisBeltUnit.transform.position);
        Vector3 unitForwardDirFlat = new Vector3(currUnitForwardDir.x, 0, currUnitForwardDir.z);
        Vector3 toCursorFlat = new Vector3(toCursor.x, 0, toCursor.z);
        float diffHorizAngle = Vector3.SignedAngle(unitForwardDirFlat, toCursorFlat, Vector3.up);
        Quaternion tmpQuant = Quaternion.Euler(0, diffHorizAngle, 0);
        Vector3 toCursorRotated = tmpQuant * toCursor;
        float diffVertAngle = Vector3.SignedAngle(currUnitForwardDir, toCursor, thisBeltUnit.transform.right);

        // HORIZONTAL (BELT) SPIN CALCULATION
        if (Math.Abs(diffHorizAngle) > beltParams.beltSpinPrecision)
        {
            beltSpin = diffHorizAngle;
            if (Math.Abs(beltSpin) > beltParams.beltMaxSpin * (1 - 0.2f * beltParams.hits))
                beltSpin = beltParams.beltMaxSpin * Math.Sign(beltSpin) * (1 - 0.2f * beltParams.hits);

        }
        else
            beltSpin = 0;

        // VERTICAL (UNIT) SPIN CALCULATION
        if (Math.Abs(diffVertAngle) > beltParams.unitSpinPrecision)
        {
            unitSpin = diffVertAngle;
            if (Math.Abs(unitSpin) > beltParams.unitMaxSpin * (1 - 0.2f * beltParams.hits))
                unitSpin = beltParams.unitMaxSpin * Math.Sign(unitSpin) * (1 - 0.2f * beltParams.hits);

        }
        else
            unitSpin = 0;
    }


    // ROTATION OF BELT AND UNIT THROUGH THEIR TRANSFORMS
    public void TurnBeltAndUnit()
    {
        transform.Rotate(new Vector3(0, beltSpin, 0));

        float tmpAngle = thisBeltUnit.transform.rotation.eulerAngles.x;
        if (tmpAngle > 180)
            tmpAngle -= 360;

        if (tmpAngle + unitSpin > beltParams.unitMaxAngle)
        {
            unitSpin = beltParams.unitMaxAngle - tmpAngle;
        }

        if (tmpAngle + unitSpin < beltParams.unitMinAngle)
        {
            unitSpin = beltParams.unitMinAngle - tmpAngle;
        }

        thisBeltUnit.transform.Rotate(new Vector3(unitSpin, 0, 0));
    }


    // FIRING CURRENT BELT
    public void Fire()
    {
        if (unitReloaded)
        {
            // SHOT CAN BE DONE
            lastShotTime = Time.time;
            unitReloaded = false;
            unitReloadSounded = false;
            //Vector3 projectileGlobalPos = thisBeltUnit.transform.TransformPoint(unitDirTransf.position);

            // MAKING SOUND
            unitSound.PlayOneShot(beltParams.unitParams.unitSound, beltParams.unitParams.unitSoundVolume);

            // CREATING PROJECTILE OBJECT
            Quaternion fireSpread = Quaternion.Euler(UnityEngine.Random.Range(-beltParams.unitParams.fireSpreadAngle, beltParams.unitParams.fireSpreadAngle), UnityEngine.Random.Range(-beltParams.unitParams.fireSpreadAngle, beltParams.unitParams.fireSpreadAngle), 0);
            GameObject newProjectile = Instantiate(projectilePrefab, unitDirTransf.position, unitDirTransf.rotation * fireSpread, null);
            newProjectile.tag = "FlyingProjectile";

            // CUSTOMIZATION OF PROJECTILE CLASH LIGHT
            Light[] lights = newProjectile.GetComponentsInChildren<Light>();
            foreach (Light light in lights)
            {
                if (light.tag == "ClashLight")
                {
                    light.intensity = beltParams.unitParams.explosionLightIntensity;
                    light.range = beltParams.unitParams.explosionLightRadius;
                }
            }

            // TRANSFERRING PARAMETERS TO SCRIPT
            ProjectileControl newProjectileControl = newProjectile.GetComponent<ProjectileControl>();
            newProjectileControl.parentUnitParams = beltParams.unitParams;
            newProjectileControl.poleModif = poleModif;
            newProjectileControl.infoText = globalObjects.infoText;

            // GENERATING RECOIL
            parentPole.GetComponent<PoleControl>().recoilSpeed = -newProjectile.transform.forward.normalized * beltParams.unitParams.recoil;
        }
    }


    // RECIEVING DAMAGE FROM ANY SOURCE
    public void RecieveDamage(string hitPartTag, float projectileDamage)
    {
        float damage = 0;
        damage = projectileDamage * 100 / (0.1f + beltParams.resistance);
        float emissionRate = beltParams.hits * 1f;
        if (UnityEngine.Random.Range(0, 99) < damage)
        {
            // PART HIT
            beltParams.hits++;
            switch (beltParams.hits)
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
            var beltDamagedLightningEmission = beltDamagedLightning.GetComponent<ParticleSystem>().emission;
            beltDamagedLightningEmission.enabled = true;
            beltDamagedLightningEmission.rateOverTime = emissionRate;
        }
    }
}

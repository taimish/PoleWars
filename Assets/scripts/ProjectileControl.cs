using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ProjectileControl : MonoBehaviour
{
    // PUBLIC
    public UnitParams parentUnitParams;
    public PoleModification poleModif;
    public Text infoText;

    // PRIVATE
    private float path = 0;
    private float fireTime;

    // START
    void Start ()
    {
        fireTime = Time.time;
    }


    // UPDATE
    void Update ()
    {
	}


    // FIXED UPDATE
    void FixedUpdate()
    {
        float currentSpeed = parentUnitParams.projectileSpeed * poleModif.projectileSpeedCoef;

        // FIRING RAYCAST TO DETECT FUTURE COLLISION
        RaycastHit hitInfo;
        int raycastLayerMask = 1 << 8;
        raycastLayerMask = ~raycastLayerMask;
        //if ((Physics.Raycast(transform.position, transform.forward, out hitInfo, currentSpeed, raycastLayerMask)) && hitInfo.collider.isTrigger)
        //if (Physics.Raycast(transform.position, transform.forward, out hitInfo, currentSpeed, raycastLayerMask))
        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, currentSpeed)) 
        {
            transform.position = hitInfo.point;
            OnContact(hitInfo.collider.gameObject);
        }
        else
        {
            transform.Translate(0, 0, currentSpeed, Space.Self);
            path += currentSpeed;
            if ((path > parentUnitParams.travelDistance) || (Time.time - fireTime > parentUnitParams.travelTime * poleModif.projectileTravelTimeCoef))
                WasteDestruction();
        }
    }





    // ********************************************************************************************************
    // OTHER METHODS ******************************************************************************************
    // ********************************************************************************************************


    // ON CONTACT
    void OnContact(GameObject hitObject)
    {
        bool damageInflicted = false;

        string hitPartTag = hitObject.tag;
        //infoText.text = "Hit tag: " + hitPartTag;

        Transform parentTransform = hitObject.transform;

        // SEARCHING FOR TOP PARENT OBJECTS
        while (parentTransform.parent != null)
            parentTransform = parentTransform.parent;

        // CHECKING IF DIDN'T HIT SELF
        List<string> noClashTags = new List<string>();
        noClashTags.Add(parentUnitParams.parentPoleID.ToString());
        noClashTags.Add("FlyingProjectile");

        if (!noClashTags.Contains(parentTransform.gameObject.tag))
        {
            ClashDestruction();

            while (!damageInflicted)
            {
                // CHECKING IF OBJECT HAS A SCRIPT
                MonoBehaviour script = hitObject.GetComponent<MonoBehaviour>();
                if (script != null)
                {
                    // CHECKING IF SCRIPT HAS RECIEVEDAMAGE METHOD
                    MethodInfo damageMethod = script.GetType().GetMethod("RecieveDamage");
                    if (damageMethod != null)
                    {
                        // INVOKING HIT OBJECT SCRIPT RECIEVEDAMAGE METHOD WITH PROJECTILE DAMAGE
                        damageMethod.Invoke(script, parameters: new object[] { hitPartTag, parentUnitParams.projectileDamageProbability });
                        damageInflicted = true;
                    }
                }

                if (hitObject.transform.parent != null)
                    hitObject = hitObject.transform.parent.gameObject;
                else
                {
                    if (!damageInflicted)
                        break;
                }
            }


            /*MonoBehaviour[] scripts = hitObject.GetComponents<MonoBehaviour>();
            if (scripts.Length > 0)
            {
                // CHECKING IF SCRIPT HAS RECIEVEDAMAGE METHOD
                MethodInfo damageMethod = scripts[0].GetType().GetMethod("RecieveDamage");
                if (damageMethod != null)
                {
                    // INVOKING HIT OBJECT SCRIPT RECIEVEDAMAGE METHOD WITH PROJECTILE DAMAGE
                    damageMethod.Invoke(scripts[0], parameters: new object[] { hitPartTag, });
                }
            }*/
        }
    }





    // SELF DESTRACTION BECAUSE OF LONG FLIGHT
    private void WasteDestruction()
    {
        Destroy(gameObject);
    }




    // SELF DESTRACTION BECAUSE OF CLASH
    private void ClashDestruction()
    {
        // CREATING CLASH EFFECT
        GameObject clashEffect = Instantiate(parentUnitParams.clashPrefab, transform.position - transform.forward.normalized * 0.1f, transform.rotation);
        clashEffect.transform.forward = -transform.forward;
        WasteDestruction();
    }

}

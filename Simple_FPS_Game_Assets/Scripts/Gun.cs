using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class Gun : MonoBehaviour
{
    [Header("Shared stuff")]
    [SerializeField] private bool isAuto = true;
    [Header("Hitscan stuff")]
    [SerializeField] private bool useHitscan = true;
    [SerializeField] private bool useInstantHitscan = true;
    [SerializeField] private float hitscanBulletSpeed = 20f;
    [SerializeField] private float hitscanDamage = 25f;
    [SerializeField] private float hitscanRange = 200f;
    [SerializeField] private float fireRate = 0.25f;

    [Header("Projectile stuff")]
    [SerializeField] private GameObject proj;
    [SerializeField] private bool useProjectileGavity = true;
    [SerializeField] private float rangeProjectileSpeed = 100f;

    private bool canShoot = true;
    private float shootTimer;

    public string gunName;
    private GameObject ammoPool;
    private ObjectPool ammoObjectPool;
    private GameObject trailPool;
    private ObjectPool trailObjectPool;
    [SerializeField] private int ammoCount = 15;
    private int maxGunAmmo;
    private int curGunAmmo;

    [SerializeField] private float reloadTime = 2f;
    private bool canReload = false;
    private bool isReloading = false;
    private float rTimer;
    [SerializeField] private Transform barrelCheckPos;

    private AimDownSights aimDownSights;
    private FPS_Controller fpsController;
    private AimGunAtRaycast aimGunAtRaycast;
    [SerializeField] private bool canADS = true;
    [Header("Lower is higer zoom")]
    [SerializeField] private float adsZoom = 60f;
    [SerializeField] private Vector3 adsOffset = Vector3.zero;
    private bool isAimingDownSight = false;
    private Vector3 posToShootFrom;
    private PlayerManager playerManager;
    private ExplosiveDamagePool explosiveDamagePool;


    private void Awake()
    {
        if(GetComponent<ExplosiveDamagePool>() != null)
        {
            explosiveDamagePool = GetComponent<ExplosiveDamagePool>();
        }
        playerManager = GetComponentInParent<PlayerManager>();
        aimGunAtRaycast = FindObjectOfType<AimGunAtRaycast>();
        fpsController = GetComponentInParent<FPS_Controller>();
        aimDownSights = GetComponentInParent<AimDownSights>();

        //set ammo
        maxGunAmmo = ammoCount;
        curGunAmmo = maxGunAmmo;

        //ammo pool
        ammoPool = new GameObject();//spawns
        ammoObjectPool = ammoPool.AddComponent<ObjectPool>();//adds script
        ammoObjectPool.prefab = proj;//set pool prefab

        //set explosive damage pool
        if(explosiveDamagePool != null )
        {
            Projectile projectile = proj.GetComponent<Projectile>();
            projectile.explosiveDamagePool = explosiveDamagePool;
        }

        ammoObjectPool.poolSize = ammoCount; //set size of pool
        ammoObjectPool.gameObject.name = gunName + " AmmoPool";

        if(useHitscan == true)
        {
            //trail pool 
            trailPool = new GameObject();//spawns
            trailObjectPool = trailPool.AddComponent<ObjectPool>();//adds script
            trailObjectPool.prefab = playerManager.bulletTrail;//set pool prefab
            trailObjectPool.poolSize = ammoCount; //set size of pool
            trailObjectPool.gameObject.name = gunName + "TrailPool";
        }
    }
    private void OnEnable()
    {
        isAimingDownSight = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * hitscanRange);
    }
    void Update()
    {
        if(PauseMenu.gameIsPaused == false)
        {
            PlayerManager.aimingDownSights = isAimingDownSight;
            PlayerManager.currentAmmo = curGunAmmo;
            PlayerManager.currentMaxAmmo = ammoCount;
            PlayerManager.isReloading = isReloading;

            if (canADS)
            {
                if (Input.GetMouseButton(1) && isReloading == false)
                {
                    if (isAimingDownSight == false)
                    {
                        float result = Mathf.InverseLerp(0, 60, adsZoom);
                        fpsController.lookSpeed = result * (fpsController.lookSpeed);

                        aimDownSights.mainCamera.fieldOfView = adsZoom;
                        aimDownSights.gunCamera.fieldOfView = adsZoom;
                        aimGunAtRaycast.enabled = false;
                        aimDownSights.gunRoot.transform.position = aimDownSights.adsPos.transform.position + adsOffset;
                        aimDownSights.gunRoot.transform.rotation = Quaternion.LookRotation(aimDownSights.adsPos.transform.forward);

                        isAimingDownSight = true;
                    }
                }
                else
                {
                    DisableAds();
                }
            }
            else
            {
                DisableAds();
            }


            if (canShoot == false)
            {
                shootTimer += Time.deltaTime;

                if (shootTimer > fireRate)
                {
                    canShoot = true;
                    shootTimer = 0;
                }
            }

            //reloading
            if (isReloading == true)
            {
                rTimer += Time.deltaTime;
                //while reloading
                aimGunAtRaycast.enabled = false;
                aimDownSights.gunRoot.transform.position = aimDownSights.reloadPos.transform.position + adsOffset;
                aimDownSights.gunRoot.transform.rotation = Quaternion.LookRotation(aimDownSights.reloadPos.transform.forward);

                //when reloading is done
                if (rTimer > reloadTime)
                {
                    isReloading = false;

                    aimDownSights.gunRoot.transform.position = aimDownSights.gunPos.transform.position;
                    aimGunAtRaycast.enabled = true;

                    curGunAmmo = maxGunAmmo;
                    rTimer = 0;
                }
            }

            if (canReload == true)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    isReloading = true;
                    canReload = false;
                }
            }

            if (curGunAmmo > 0)
            {
                if (Physics.Linecast(barrelCheckPos.position, transform.position, playerManager.blockingLayer))
                {
                    Debug.Log("blocked");
                }
                else
                {
                    ShootGun();
                }
            }

            if (curGunAmmo < maxGunAmmo)
            {
                canReload = true;
            }
        }
  
    }
    private void ShootGun()
    {
        if (isAuto)
        {

            if (canShoot && isReloading == false && Input.GetMouseButton(0))
            {
                if (useHitscan)
                {
                    FireHitscan();
                }
                else
                {
                    CreateProjectile();
                }
                canShoot = false;
            }
        }
        else
        {

            if (canShoot && isReloading == false && Input.GetMouseButtonDown(0))
            {
                if (useHitscan)
                {
                    FireHitscan();
                }
                else
                {
                    CreateProjectile();
                }
                canShoot = false;
            }
        }
    }
    private void CreateProjectile()
    {
        GameObject proj = ammoObjectPool.GetObject();
        proj.transform.position = transform.position;
        proj.transform.rotation = Quaternion.LookRotation(transform.forward);


        //GameObject proj = Instantiate(proj, transform.position, Quaternion.identity);

        Rigidbody rb = proj.GetComponent<Rigidbody>();

        if (useProjectileGavity)
        {
            rb.useGravity = true;
        }
        else
        {
            rb.useGravity = false;
        }

        rb.AddForce(transform.forward *  rangeProjectileSpeed, ForceMode.Force);

        curGunAmmo--;
    }
    private void FireHitscan()
    {

        if(isAimingDownSight)
        {
            posToShootFrom = Camera.main.transform.position;
        }
        else
        {
            posToShootFrom = transform.position;

        }

        if(Physics.Raycast(posToShootFrom, transform.forward, out RaycastHit hit, hitscanRange, playerManager.hitscanlayers))
        {
            if(useInstantHitscan)
            {
                GameObject trail = trailObjectPool.GetObject();
                trail.transform.position = transform.position;
                trail.transform.rotation = transform.rotation;

                StartCoroutine(SpawnInstantTrail(trail.GetComponent<TrailRenderer>(), hit.point)); // Start the coroutine

                if (hit.collider.GetComponent<EnemyHealth>() != null)
                {
                    EnemyHealth eh = hit.collider.GetComponent<EnemyHealth>();
                    eh.health -= hitscanDamage;
                }
            }
            else //hitscan over travel time
            {
                GameObject trail = trailObjectPool.GetObject();
                trail.transform.position = transform.position;
                trail.transform.rotation = transform.rotation;

                StartCoroutine(SpawnTravelTrail(trail.GetComponent<TrailRenderer>(), hit)); // Start the coroutine

            }
        }
        else//miss raycast
        {
            Vector3 misseverything = transform.position + transform.forward * hitscanRange;
            GameObject trail = trailObjectPool.GetObject();
            trail.transform.position = transform.position;
            trail.transform.rotation = transform.rotation;

            StartCoroutine(SpawnMissTrail(trail.GetComponent<TrailRenderer>(), misseverything)); // Start the coroutine
        }

        curGunAmmo--;
    }

    private void DisableAds()
    {
        //reset look speed
        fpsController.lookSpeed = fpsController.startLookSpeed;

        //reset the fov
        aimDownSights.mainCamera.fieldOfView = 60f;
        aimDownSights.gunCamera.fieldOfView = 60f;
        //reset gun pos
        aimDownSights.gunRoot.transform.position = aimDownSights.gunPos.transform.position;
        aimGunAtRaycast.enabled = true;

        isAimingDownSight = false;

    }

    private IEnumerator SpawnInstantTrail(TrailRenderer trail, Vector3 miss)
    {
        float time = 0;
        Vector3 startpos = trail.transform.position;

        while (time < 0.05f)
        {
            trail.transform.position = Vector3.Lerp(startpos, miss, time);

            time += Time.deltaTime;

            yield return null;
        }
        trail.transform.position = miss;

        //Destroy(trail.gameObject, trail.time);
    }
    private IEnumerator SpawnMissTrail(TrailRenderer trail, Vector3 miss)
    {
        float time = 0;
        Vector3 startpos = trail.transform.position;

        while (time < 0.15f)
        {
            trail.transform.position = Vector3.Lerp(startpos, miss, time);

            time += Time.deltaTime;

            yield return null;
        }
        trail.transform.position = miss;

        //Destroy(trail.gameObject, trail.time);
    }
    private IEnumerator SpawnTravelTrail(TrailRenderer Trail, RaycastHit hit)
    {
        Vector3 direction = (hit.point - Trail.transform.position).normalized;

        float distance = Vector3.Distance(Trail.transform.position, hit.point);
        float startingDistance = distance;

        while (distance > 0)
        {
            Trail.transform.position = Vector3.Lerp(posToShootFrom, hit.point, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * hitscanBulletSpeed;

            yield return null;
        }

        Trail.transform.position = hit.point;

        //hitscan dmg
        if (hit.point != null)
        {
            if (hit.collider.GetComponent<EnemyHealth>() != null)
            {
                EnemyHealth eh = hit.collider.GetComponent<EnemyHealth>();
                eh.health -= hitscanDamage;
            }
        }
        //Destroy(Trail.gameObject, Trail.time);
    }
}

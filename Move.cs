using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] float movementSpeed = 5f;
    [SerializeField] float shiftSpeed = 10f;
    [SerializeField] float jumpForce = 7f;
    TextUpdate textUpdate;
    [SerializeField] GameObject damageUi;
    [SerializeField] Animator anim;
    [SerializeField] GameObject pistol, rifle, miniGun;
    [SerializeField] Image pistolUI, rifleUI, miniGunUI, cusror;
    //Ссылка на источник звука
    [SerializeField] AudioSource characterSounds;
    //Ссылка на звук прыжка
    [SerializeField] AudioClip jump;
    private int health;
    bool isPistol, isRifle, isMiniGun;
    bool isGrounded = true;
    public bool dead;
    float currentSpeed;
    float stamina = 5f;
    Rigidbody rb;
    GameManager gameManager;
    Vector3 direction;
    public enum Weapons
    {
        None,
        Pistol,
        Rifle,
        MiniGun
    }
    Weapons weapons = Weapons.None;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        //gameManager.ChangePlayersList();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        currentSpeed = movementSpeed;
        health = 110;
        textUpdate = GetComponent<TextUpdate>();
        //Если персонаж не наш, то...
        if (!photonView.IsMine)
        {
            //Находим камеру в иерархии игрока и отключаем её
            transform.Find("Main Camera").gameObject.SetActive(false);
            transform.Find("Canvas").gameObject.SetActive(false);
            //отключаем скрипт PlayerController
            this.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        direction = new Vector3(moveHorizontal, 0.0f, moveVertical);
        direction = transform.TransformDirection(direction);
        if (direction.x != 0 || direction.z != 0)
        {
            anim.SetBool("Run", true);
            //Если источник звука не воспроизводит звук и мы на земле, то...
            if (!characterSounds.isPlaying && isGrounded)
            {
                //Включаем звук
                characterSounds.Play();
            }
        }
        if (direction.x == 0 && direction.z == 0)
        {
            anim.SetBool("Run", false);
            //Выключаем звук
            characterSounds.Stop();
        }
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            isGrounded = false;
            //Отключаем звук бега
            characterSounds.Stop();
            //Создаем временный источник звука для прыжка
            AudioSource.PlayClipAtPoint(jump, transform.position);
            anim.SetBool("Jump", true);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (stamina > 0)
            {
                stamina -= Time.deltaTime;
                currentSpeed = shiftSpeed;
            }
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            stamina += Time.deltaTime;
            currentSpeed = movementSpeed;
        }
        if (stamina > 5f)
        {
            stamina = 5f;
        }
        else if (stamina < 0)
        {
            stamina = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1) && isPistol)
        {
            ChooseWeapon(Weapons.Pistol);
            //photonView.RPC("ChooseWeapon", RpcTarget.All, Weapons.Pistol);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && isRifle)
        {
            ChooseWeapon(Weapons.Rifle);
            //photonView.RPC("ChooseWeapon", RpcTarget.All, Weapons.Rifle);
        }
        //Здесь допиши логику для минигана и для отсутствия оружия
        if (Input.GetKeyDown(KeyCode.Alpha3) && isMiniGun)
        {
            ChooseWeapon(Weapons.MiniGun);
            //photonView.RPC("ChooseWeapon", RpcTarget.All, Weapons.MiniGun);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ChooseWeapon(Weapons.None);
            //photonView.RPC("ChooseWeapon", RpcTarget.All, Weapons.None);
        }
    }
    void FixedUpdate()
    {
        rb.MovePosition(transform.position + direction * currentSpeed * Time.deltaTime);
    }
    public void GetDamage(int count)
    {
        photonView.RPC("ChangeHealth", RpcTarget.All, count);
    }
    [PunRPC]
    public void ChangeHealth(int count)
    {
        //вычитаем здоровье
        health -= count;
        textUpdate.SetHealth(health);
        damageUi.SetActive(true);
        Invoke("RemoveDamageUi", 0.1f);
        //если здоровье меньше либо равно нулю, то...
        if (health <= 0)
        {
            dead = true;
            //Активируем анимацию смерти
            anim.SetBool("Die", true);
            transform.Find("Main Camera").GetComponent<ThirdPersonCamera>().isSpectator = true;
            //Убираем оружие
            ChooseWeapon(Weapons.None);
            gameManager.ChangePlayersList();
            //отключаем скрипт PlayerController, чтобы персонаж не мог передвигаться
            this.enabled = false;
        }
    }
    void RemoveDamageUi()
    {
        damageUi.SetActive(false);
    }
    void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
        anim.SetBool("Jump", false);
    }
    [PunRPC]
    public void ChooseWeapon(Weapons weapons)
    {
        anim.SetBool("Pistol", weapons == Weapons.Pistol);
        anim.SetBool("Assault", weapons == Weapons.Rifle);
        anim.SetBool("MiniGun", weapons == Weapons.MiniGun);
        anim.SetBool("NoWeapon", weapons == Weapons.None);
        pistol.SetActive(weapons == Weapons.Pistol);
        rifle.SetActive(weapons == Weapons.Rifle);
        miniGun.SetActive(weapons == Weapons.MiniGun);
        if (weapons != Weapons.None)
        {
            cusror.enabled = true;
        }
        else
        {
            cusror.enabled = false;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        switch (other.gameObject.tag)
        {
            case "pistol":
                if (!isPistol)
                {
                    isPistol = true;
                    pistolUI.color = Color.white;
                    ChooseWeapon(Weapons.Pistol);
                }

                break;
            case "rifle":
                if (!isRifle)
                {
                    isRifle = true;
                    rifleUI.color = Color.white;
                    ChooseWeapon(Weapons.Rifle);
                }
                break;
            //здесь напиши логику для минигана
            case "minigun":
                if (!isMiniGun)
                {
                    isMiniGun = true;
                    miniGunUI.color = Color.white;
                    ChooseWeapon(Weapons.MiniGun);
                }
                break;
            default:
                break;

        }
        Destroy(other.gameObject);

    }
}
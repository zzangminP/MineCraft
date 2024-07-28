using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//
// Player�� ������, ��������, ���� ��ġ �� �ı��� ����
// Rigidbody�� ���������� ���� �ʰ� ���� �ۼ�...
// World�� Collider�� ���� Ȱ��ȭ��ä�� ������ �����Ѵٸ� ������ ���ɷ��� 
// �÷��̾ ������ �κи� Collider�� Ȱ��ȭ �Ҳ���
//
//
//
//
//
//
//

public class Player : MonoBehaviour
{
    private Transform cam;
    private World world;

    // ------------ĳ���� ������---------------//
    public bool isGrounded;
    public bool isSprinting;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.3f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;

    //[SerializeField] private float mouseSensitive;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;
    [HideInInspector]
    public bool isOpeningUI = false;
    //-------------������ �� -----------------//



    // ���� �ı� �� ��ġ ���� ������
    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Text selectedBlockText;
    public byte selectedBlockIndex = 1; // 0 �� ������� -> isSolid �� �ƴ�

    public int orientation;


    //--------------UI?------------------------//
    public GameObject ui_craftTable;
    public GameObject ui_chest;
    public GameObject ui_furnance;
    //--------------UI end---------------------//


    // �ʱ�ȭ
    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        UIInit();



        // ���콺 Ŀ���� Game ������ �ȳ����� ����
        Cursor.lockState = CursorLockMode.Locked;

        selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " block selected";
    }

    private void UIInit()
    {
        ui_craftTable = GameObject.Find("CraftTableUI");
        ui_chest = GameObject.Find("ChestUI");
        ui_furnance = GameObject.Find("FurnanceUI");


        ui_craftTable.SetActive(false);
        ui_chest.SetActive(false);
        ui_furnance.SetActive(false);
    }



    // ������ �ð����� ȣ��, ���ʿ��� ���� ���̰� ��Ȯ�� ��������
    private void FixedUpdate()
    {
        CalculateVelocity();
        if (jumpRequest)
            Jump();

        transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
        cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);
        transform.Translate(velocity, Space.World);
    }



    // �� �����Ӹ��� ȣ��, �񹰸��� ����, �ε巯�� ������
    private void Update()
    {
        GetPlayerInputs();
        PlaceCursorBlocks();
        SetDirection();
    }




    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }


    void SetDirection()
    {
        Vector3 XZDirection = transform.forward;
        XZDirection.y = 0;
        if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
            orientation = 0;
        else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
            orientation = 5;
        else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
            orientation = 1;
        else
            orientation = 4;
    }



    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        // ���������� Ž���� ��ġ�� ������ ����;
        Vector3 lastPos = new Vector3();

        // reach �Ÿ���ŭ �ݺ�
        while (step < reach)
        {

            // ī�޶��� ��ġ���� ī�޶� �ٶ󺸰� �ִ� �������� Step �Ÿ���ŭ �̵�
            Vector3 pos = cam.position + (cam.forward * step);

            // ���� �ű⿡ Voxel�� �ִٸ�
            if (world.CheckForVoxel(pos))
            {


                // ȭ�鿡 ǥ���ε� ��ǥ�� �ȸ¾Ƽ� ���� �����ϰ� �س���;
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;
                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                // ���� Ž�������Ƿ� �޼��� ����
                return;
            }
            // ���� ��ġ�� ������ ��ġ�� ������Ʈ
            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            // Ž�� ������ ������Ŵ;
            step += checkIncrement;
        }
        // reach �Ÿ� ���� ������ ������ ��Ȱ��ȭ
        // ��� ���� ������ 100���� �ؼ� �Ⱥ���
        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);

    }






    private void CalculateVelocity()
    {

        // �߷� ���
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;


        // �ȱ� & �޸���
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;

        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        //�յ��¿� �˻� 
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;

        // �������ų� �ö󰥶�
        if (velocity.y < 0)
            velocity.y = CheckDownSpeed(velocity.y);
        else if (velocity.y > 0)
            velocity.y = CheckUpSpeed(velocity.y);
    }





    // ĳ���� ��Ʈ�� ��ǲ
    private void GetPlayerInputs()
    {
        if (isOpeningUI == false)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            mouseHorizontal = Input.GetAxis("Mouse X");
            mouseVertical = Input.GetAxis("Mouse Y");



            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();



            if (Input.GetButtonDown("Sprint"))
                isSprinting = true;
            if (Input.GetButtonUp("Sprint"))
                isSprinting = false;

            if (isGrounded && Input.GetButtonDown("Jump"))
                jumpRequest = true;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                if (scroll > 0)
                    selectedBlockIndex++;
                else
                    selectedBlockIndex--;

                if (selectedBlockIndex > (byte)(world.blockTypes.Length - 1))
                    selectedBlockIndex = 1;
                if (selectedBlockIndex < 1)
                    selectedBlockIndex = (byte)(world.blockTypes.Length - 1);

                selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " block selected";
            }

            if (highlightBlock.gameObject.activeSelf)
            {


                // ���� �ı� Destroy block
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3 destroyPos = highlightBlock.position;
                    byte destroyedBlockID = world.GetChunkFromVector3(destroyPos).EditVoxel(destroyPos, 0);

                    GameObject popObjectPrefab = Resources.Load<GameObject>("PopObject");

                    GameObject popObjectInstance = Instantiate(popObjectPrefab, destroyPos, Quaternion.identity);
                    PopObject popObject = popObjectInstance.GetComponent<PopObject>();

                    popObject.Initialize(world, destroyPos, destroyedBlockID);
                }




                // ���� ��ġ Place block
                if (Input.GetMouseButtonDown(1))
                {
                    switch(world.GetChunkFromVector3(highlightBlock.position).CheckBlockID(highlightBlock.position))
                    {
                        // chest
                        case 16:
                            ui_chest.SetActive(true);
                            break;

                        // Furnance
                        case 18:
                            ui_furnance.SetActive(true);
                            break;

                        // craftTable
                        case 19:
                            ui_craftTable.SetActive(true);
                            break;
                        default:
                            world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, selectedBlockIndex);
                            break;
                    }



                }



            }
        }
        else
        {
            return;
        }
    }

    // �������°� ���
    private float CheckDownSpeed(float downSpeed)
    {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)))
        {
            if (world.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)))
            {
                isGrounded = false;
                return downSpeed;

            }
            else
            {
                isGrounded = true;
                return 0;
            }
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    // �ö󰡴°� ���
    private float CheckUpSpeed(float upSpeed)
    {
        if (world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)))
        {
            if (world.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
                world.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)))
                return upSpeed;
            else
                return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    /// ============================================================================ //
    /// =========================�� �� �� �� ���� �˻�=============================== //
    /// ============================================================================ //
    ///                    true �� ������ �ִٴ� ��
    ///                    false �� ������ ���ٴ� ��




    public bool front
    {
        get
        {
            if (World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
               World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
            {
                if (World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth))||
                    World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
                    return false;
                else
                    return true;

            }
            else
            {
                return false;
            }    

            //return World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
            //       World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth));
        }
    }
    public bool back
    {
        get
        {

            if (World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
            {
                if (World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                    World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
                    return false;
                else
                    return true;

            }
            else
            {
                return false;
            }
            //return World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
            //       World.Instance.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth));
        }
    }
    public bool left
    {
        get
        {

            if (World.Instance.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                World.Instance.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                if (World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                    World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
                    return false;
                else
                    return true;

            }
            else
            {
                return false;
            }



            //return world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
            //       world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z));
        }
    }
    public bool right
    {
        get
        {

            if (World.Instance.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                World.Instance.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                if (World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                    World.Instance.CheckWaterForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
                    return false;
                else
                    return true;

            }
            else
            {
                return false;
            }



            //return world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
            //       world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z));
        }
    }
}
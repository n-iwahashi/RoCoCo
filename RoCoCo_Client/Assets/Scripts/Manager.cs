using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject rendererPrefab;
    public Sprite boxSprite;
    public Sprite circleSprite;

    GameObject agent1;
    GameObject agent2;
    public bool isAgentConnect = false;
    public float tl = 1.0f;

    List<Component> agentComponents = new List<Component>();
    List<GameObject> objects = new List<GameObject>();

    string guiStatusMsg;
    GUIStyle guiStyle = new GUIStyle();

    public static Color32 COLOR_INIT = new Color32(0x77, 0x88, 0x99, 0xff); // light slate gray
    public static Color32 COLOR_PLAY = new Color32(0x31, 0x4d, 0x79, 0xff); // fun blue
    public static Color32 COLOR_END = new Color32(0x00, 0x00, 0x00, 0xff);

    void Awake()
    {
        Screen.SetResolution(AppSettings.SCREEN_WIDTH, AppSettings.SCREEN_HEIGHT, false);
    }

    void Start()
    {
        agent1 = GameObject.Find("Agent1");
        agent2 = GameObject.Find("Agent2");
        
        guiStyle.normal.textColor = Color.white;
        guiStyle.fontSize = 24;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(5, 5, 500, 20), guiStatusMsg, guiStyle);
    }

    public void UpdateGuiMsg(string msg)
    {
        guiStatusMsg = msg;
    }

    public GameObject CreateRendererObject(string shape)
    {
        GameObject obj = Instantiate(rendererPrefab) as GameObject;
        if (shape == "box")
        {
            obj.GetComponent<SpriteRenderer>().sprite = boxSprite;
        }
        else if (shape == "circle")
        {
            obj.GetComponent<SpriteRenderer>().sprite = circleSprite;
        }
        else
        {
            UpdateGuiMsg("Invalid shape:" + shape);
        }
        return obj;
    }

    public void Init(InitReply initReply)
    {
        guiStatusMsg = "";
        isAgentConnect = false;

        // camera
        _InitCamera(initReply.camera);

        // clear agents and objects
        foreach (Component comp in agentComponents)
        {
            Destroy(comp);
        }
        agentComponents.Clear();
        foreach (GameObject gameObj in objects)
        {
            Destroy(gameObj);
        }
        objects.Clear();

        // arrow
        agent1.transform.Find("Vector").GetComponent<SpeedVector>().SetRatio(AppSettings.Agent1Arrow);
        agent2.transform.Find("Vector").GetComponent<SpeedVector>().SetRatio(AppSettings.Agent2Arrow);

        GameObject arrow1 = GameObject.Find("Arrow1");
        if (arrow1)
        {
            arrow1.transform.Find("Vector").GetComponent<SpeedVector>().SetRatio(AppSettings.Agent1Arrow);
        }
        GameObject arrow2 = GameObject.Find("Arrow2");
        if (arrow2)
        {
            arrow2.transform.Find("Vector").GetComponent<SpeedVector>().SetRatio(AppSettings.Agent2Arrow);
        }

        // initialize agents and objects
        _InitAgent(agent1, initReply.agents[0]);
        _InitAgent(agent2, initReply.agents[1]);

        foreach (Obj obj in initReply.objects)
        {
            _InitObject(obj);
        }
    }

    void _InitCamera(CameraObj cam)
    {
        _InitCamera(GameObject.Find("Camera01"), cam, AppSettings.Camera1Axis);
        _InitCamera(GameObject.Find("Camera02"), cam, AppSettings.Camera2Axis);
        _InitCamera(GameObject.Find("Camera1"), cam, AppSettings.Camera1Axis);
        _InitCamera(GameObject.Find("Camera2"), cam, AppSettings.Camera2Axis);
    }

    void _InitCamera(GameObject camera, CameraObj cam, int cameraAxis)
    {
        if (camera == null) return;

        camera.transform.position = new Vector3(cam.pos[0], cam.pos[1], -10.0f);
        camera.GetComponent<Camera>().orthographicSize = cam.size;
        AppSettings.cameraSize = cam.size;
        if (!string.IsNullOrEmpty(cam.background))
        {
            camera.GetComponent<Camera>().backgroundColor = AppUtil.ToColor(cam.background);
        }

        if (cameraAxis == 1)
        {
            // 左右反転
            camera.GetComponent<CameraScale>().scale = new Vector3(-1.0f, 1.0f, 1.0f);
        }
        else if (cameraAxis == 2)
        {
            // 上下反転
            camera.GetComponent<CameraScale>().scale = new Vector3(1.0f, -1.0f, 1.0f);
        }
        else if (cameraAxis == 3)
        {
            // 上下左右反転
            camera.GetComponent<CameraScale>().scale = new Vector3(-1.0f, -1.0f, 1.0f);
        }
        else
        {
            // 通常
            camera.GetComponent<CameraScale>().scale = new Vector3(1.0f, 1.0f, 1.0f);
        }
    }

    public void SetCameraBkColor(Color color)
    {
        _SetCameraBkColor(GameObject.Find("Camera01"), color);
        _SetCameraBkColor(GameObject.Find("Camera02"), color);
        _SetCameraBkColor(GameObject.Find("Camera1"), color);
        _SetCameraBkColor(GameObject.Find("Camera2"), color);
    }

    void _SetCameraBkColor(GameObject camera, Color color)
    {
        if (camera == null) return;
        camera.GetComponent<Camera>().backgroundColor = color;
    }

    void _InitAgent(GameObject gameObj, Agent agent)
    {
        gameObj.name = agent.name;

        // Jointを描画するためのオブジェクトを廃棄
        Transform trans = gameObj.transform.Find("JointRenderer");
        if (trans != null)
        {
            Destroy(trans.gameObject);
        }

        if (agent.shape == "circle")
        {
            gameObj.GetComponent<SpriteRenderer>().sprite = circleSprite;
            if (agent.colider)
            {
                agentComponents.Add( gameObj.AddComponent<CircleCollider2D>() );
            }
        }
        else if (agent.shape == "box")
        {
            gameObj.GetComponent<SpriteRenderer>().sprite = boxSprite;
            if (agent.colider)
            {
                agentComponents.Add( gameObj.AddComponent<BoxCollider2D>() );
            }
        }
        gameObj.GetComponent<SpriteRenderer>().color = AppUtil.ToColor(agent.color);

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(agent.sprite))
        {
            sprite = Resources.Load<Sprite>(agent.sprite);
            if (sprite != null)
            {
                gameObj.GetComponent<SpriteRenderer>().sprite = sprite;
                float sizeX = sprite.bounds.size.x;
                float sizeY = sprite.bounds.size.y;
                gameObj.transform.localScale = new Vector3(agent.scale[0]/sizeX, agent.scale[1]/sizeY, 1.0f);
                gameObj.transform.rotation = Quaternion.Euler(0.0f, 0.0f, agent.rotation + agent.init_rotation);
            }
        }
        if (sprite == null)
        {
            gameObj.transform.localScale = new Vector3(agent.scale[0], agent.scale[1], 1.0f);        
        }
      
        Rigidbody2D rb = gameObj.transform.GetComponent<Rigidbody2D>();
        rb.position = new Vector2(agent.pos[0], agent.pos[1]);
        rb.velocity = new Vector2();
        rb.rotation = 0.0f;
    }

    void _InitObject(Obj obj)
    {
        GameObject gameObj;
        if (obj.type == "static")
        {
            gameObj = Instantiate(rendererPrefab) as GameObject;
            if (obj.shape == "circle")
            {
                gameObj.GetComponent<SpriteRenderer>().sprite = circleSprite;
                if (obj.colider)
                {
                    CircleCollider2D circleColider = gameObj.AddComponent<CircleCollider2D>();
                    gameObj.AddComponent<CircleCollider2D>();
                }
            }
            else if (obj.shape == "box")
            {
                gameObj.GetComponent<SpriteRenderer>().sprite = boxSprite;
                if (obj.colider)
                {
                    gameObj.AddComponent<BoxCollider2D>();
                }
            }
            else if (obj.shape == "line")
            {
                gameObj.GetComponent<SpriteRenderer>().sprite = boxSprite;

                Vector2 pos1 = new Vector2(obj.pos1[0], obj.pos1[1]);
                Vector2 pos2 = new Vector2(obj.pos2[0], obj.pos2[1]);

                gameObj.transform.position = (pos1 + pos2) / 2.0f;
                float angle = AppUtil.GetDeg(pos1, pos2);
                gameObj.transform.eulerAngles = Vector3.forward * angle;
                float mag = (pos1 - pos2).magnitude;
                gameObj.transform.localScale = new Vector3(obj.width, mag, 1.0f);
                if (obj.colider)
                {
                    gameObj.AddComponent<BoxCollider2D>();
                }
            }
            else
            {
                Debug.Log("Invalid shape:" + obj.shape);
                return;
            }
        }
        else if (obj.type == "connector")
        {
            gameObj = Instantiate(rendererPrefab) as GameObject;
            gameObj.GetComponent<SpriteRenderer>().sprite = boxSprite; // box
            Rigidbody2D rigid2D = gameObj.AddComponent<Rigidbody2D>();
            rigid2D.gravityScale = 0.0f;
            if (obj.colider)
            {
                gameObj.AddComponent<BoxCollider2D>();
            }

            GameObject a = GameObject.Find(obj.a);
            GameObject b = GameObject.Find(obj.b);

            Vector2 posA = a.transform.GetComponent<Rigidbody2D>().position;
            Vector2 posB = b.transform.GetComponent<Rigidbody2D>().position;
            rigid2D.position = new Vector2((posA.x + posB.x)/2.0f, (posA.y + posB.y) / 2.0f);
            Debug.Log("Connector pos:" + rigid2D.position);
            rigid2D.rotation = AppUtil.GetDeg(posA, posB); // ★初期状態で机が傾いている場合に対応

            FixedJoint2D fjA = a.AddComponent<FixedJoint2D>() as FixedJoint2D;
            fjA.connectedBody = rigid2D;
            //fjA.dampingRatio = 0.2f;
            fjA.frequency = 0;
            agentComponents.Add(fjA);
            FixedJoint2D fjB = b.AddComponent<FixedJoint2D>() as FixedJoint2D;
            fjB.connectedBody = rigid2D;
            //fjB.dampingRatio = 0.2f;
            fjB.frequency = 0;
            agentComponents.Add(fjB);

            isAgentConnect = true;
            tl = (posA - posB).magnitude;

            // Jointを描画するためのオブジェクトを追加
            _CreateJointRenderer(rigid2D, a);
            _CreateJointRenderer(rigid2D, b);
        }
        else
        {
            // invalid type
            Debug.Log("Invalid type:" + obj.type);
            return;
        }

        gameObj.name = obj.name;
        gameObj.GetComponent<SpriteRenderer>().sortingOrder = obj.order;
        if (obj.pos.Count == 2)
        {
            gameObj.transform.position = new Vector2(obj.pos[0], obj.pos[1]);
        }
        if (obj.scale.Count == 2)
        {
            gameObj.transform.localScale = new Vector3(obj.scale[0], obj.scale[1], 1.0f);
        }
        gameObj.GetComponent<SpriteRenderer>().color = AppUtil.ToColor(obj.color);

        if (obj.type == "static" && (obj.shape == "circle" || obj.shape == "box"))
        {
            Sprite sprite = null;
            if (!string.IsNullOrEmpty(obj.sprite))
            {
                sprite = Resources.Load<Sprite>(obj.sprite);
                if (sprite != null)
                {
                    gameObj.GetComponent<SpriteRenderer>().sprite = sprite;
                    float sizeX = sprite.bounds.size.x;
                    float sizeY = sprite.bounds.size.y;
                    gameObj.transform.localScale = new Vector3(obj.scale[0] / sizeX, obj.scale[1] / sizeY, 1.0f);
                    gameObj.transform.rotation = Quaternion.Euler(0.0f, 0.0f, obj.rotation);
                }
            }
        }

        objects.Add(gameObj);
    }

    private void _CreateJointRenderer(Rigidbody2D objRigid, GameObject agent)
    {
        // Jointを描画するためのオブジェクトを追加
        // サイズや座標は親オブジェクト（=エージェント）に対する割合を指定する
        GameObject jointRenderer = CreateRendererObject("circle");
        jointRenderer.transform.parent = agent.transform;
        jointRenderer.name = "JointRenderer";
        Vector2 gripDirection = objRigid.position - agent.GetComponent<Rigidbody2D>().position;
        gripDirection = gripDirection.normalized;
        jointRenderer.transform.localPosition = new Vector3(gripDirection.x * 0.6f, gripDirection.y * 0.6f, 0);
        jointRenderer.transform.localScale = new Vector3(0.3f, 0.3f, agent.transform.localScale[2]);
        jointRenderer.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, AppUtil.GetDeg(gripDirection)));
        jointRenderer.GetComponent<SpriteRenderer>().color = agent.GetComponent<SpriteRenderer>().color;
        jointRenderer.GetComponent<SpriteRenderer>().sortingOrder = agent.GetComponent<SpriteRenderer>().sortingOrder + 1;
    }
}

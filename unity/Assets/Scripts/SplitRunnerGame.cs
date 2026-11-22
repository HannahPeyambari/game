using System.Collections;
using UnityEngine;

/// <summary>Creates and runs the two-lane Twin Dash game without external assets.</summary>
public class SplitRunnerGame : MonoBehaviour
{
    private const float GroundY = 0f;
    private const float SpawnX = 11f;
    private const float PlayerX = -5f;
    private readonly float[] laneY = { 2f, -2f };
    private readonly string[] laneNames = { "SKY RUNNER", "SUNSET RUNNER" };
    private readonly Color[] runnerColors = { new Color(0.18f, 0.73f, 0.96f), new Color(1f, 0.48f, 0.27f) };
    private readonly float[] scores = new float[2];
    private readonly bool[] alive = { true, true };
    private readonly float[] speeds = { 6.2f, 6.8f };
    private readonly float[] spawnWaits = { 1.45f, 1.2f };

    private Sprite pixel;
    private GUIStyle titleStyle, scoreStyle, hintStyle, buttonStyle, deadStyle;
    private bool ready;

    private void Start()
    {
        ConfigureCamera();
        pixel = CreatePixelSprite();
        CreateBackdrop();
        for (int i = 0; i < 2; i++)
        {
            CreateRunner(i);
            StartCoroutine(SpawnObstacles(i));
        }
        ready = true;
    }

    private void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cam = cameraObject.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.2f;
        cam.backgroundColor = new Color(0.055f, 0.075f, 0.14f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    private Sprite CreatePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private GameObject MakeBlock(string objectName, Vector2 position, Vector2 size, Color color, int order)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.position = position;
        obj.transform.localScale = size;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = order;
        return obj;
    }

    private void CreateBackdrop()
    {
        MakeBlock("Upper lane", new Vector2(0, 2f), new Vector2(24, 4.3f), new Color(0.09f, 0.13f, 0.23f), -10);
        MakeBlock("Lower lane", new Vector2(0, -2f), new Vector2(24, 4.3f), new Color(0.12f, 0.12f, 0.22f), -10);
        MakeBlock("Lane divider", new Vector2(0, 0), new Vector2(24, 0.06f), new Color(0.3f, 0.36f, 0.52f), -2);
        for (int i = 0; i < 2; i++)
        {
            MakeBlock("Ground " + i, new Vector2(0, laneY[i] - 1.12f), new Vector2(24, 0.18f), new Color(0.3f, 0.83f, 0.75f), -1);
            for (int j = 0; j < 8; j++)
            {
                float x = -8.5f + j * 2.5f;
                MakeBlock("Lane dash", new Vector2(x, laneY[i] - 1.36f), new Vector2(0.9f, 0.045f), new Color(0.35f, 0.45f, 0.61f), -3);
            }
        }
    }

    private void CreateRunner(int lane)
    {
        GameObject runner = MakeBlock(laneNames[lane], new Vector2(PlayerX, laneY[lane] - 0.72f), new Vector2(0.72f, 0.82f), runnerColors[lane], 2);
        BoxCollider2D collider = runner.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        Rigidbody2D body = runner.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Runner runnerScript = runner.AddComponent<Runner>();
        runnerScript.Setup(this, lane);
    }

    private IEnumerator SpawnObstacles(int lane)
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnWaits[lane] + Random.Range(0f, 0.5f));
            if (!alive[lane]) continue;
            float height = Random.Range(0.72f, 1.2f);
            GameObject obstacle = MakeBlock("Obstacle", new Vector2(SpawnX, laneY[lane] - 1.12f + height * 0.5f), new Vector2(0.58f, height), new Color(1f, 0.36f, 0.42f), 3);
            BoxCollider2D trigger = obstacle.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            Obstacle obstacleScript = obstacle.AddComponent<Obstacle>();
            obstacleScript.Setup(this, lane);
        }
    }

    internal void Jump(int lane, Rigidbody2D body)
    {
        if (!alive[lane]) return;
        if (Mathf.Abs(body.position.y - (laneY[lane] - 0.72f)) < 0.08f)
        {
            body.velocity = new Vector2(body.velocity.x, 8.8f);
        }
    }

    internal void Hit(int lane)
    {
        if (!alive[lane]) return;
        alive[lane] = false;
    }

    internal float Speed(int lane) { return speeds[lane]; }
    internal bool IsAlive(int lane) { return alive[lane]; }

    private void Update()
    {
        if (!ready) return;
        for (int i = 0; i < 2; i++)
            if (alive[i]) scores[i] += Time.deltaTime * 10f;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
    }

    private void Restart()
    {
        StopAllCoroutines();
        for (int i = 0; i < 2; i++)
        {
            scores[i] = 0;
            alive[i] = true;
            speeds[i] = 6.2f + i * 0.6f;
        }
        foreach (Obstacle obstacle in FindObjectsOfType<Obstacle>()) Destroy(obstacle.gameObject);
        foreach (Runner runner in FindObjectsOfType<Runner>())
        {
            runner.transform.position = new Vector2(PlayerX, laneY[runner.Lane] - 0.72f);
            runner.ResetRunner();
        }
        for (int i = 0; i < 2; i++) StartCoroutine(SpawnObstacles(i));
    }

    private void OnGUI()
    {
        if (!ready) return;
        EnsureStyles();
        float scale = Mathf.Clamp(Screen.width / 1100f, 0.7f, 1.5f);
        GUI.Label(new Rect(30, 18, 520, 48), "TWIN DASH", titleStyle);
        GUI.Label(new Rect(32, 65, 480, 28), "TWO LANES. TWO JUMPS. HOW LONG CAN YOU LAST?", hintStyle);
        DrawLaneHud(0, 115, scale);
        DrawLaneHud(1, Screen.height * 0.5f + 15, scale);
        GUI.Label(new Rect(Screen.width - 250, 24, 220, 28), "W / SPACE  ·  UPPER", hintStyle);
        GUI.Label(new Rect(Screen.width - 250, 51, 220, 28), "↑  ·  LOWER     R  ·  RESET", hintStyle);
        if ((!alive[0] || !alive[1]) && GUI.Button(new Rect(Screen.width * 0.5f - 75, Screen.height * 0.5f - 25, 150, 48), "RUN AGAIN  ·  R", buttonStyle)) Restart();
    }

    private void DrawLaneHud(int lane, float top, float scale)
    {
        string status = alive[lane] ? "RUNNING" : "CRASHED";
        GUI.Label(new Rect(34, top, 240, 30), laneNames[lane] + "   /   " + status, alive[lane] ? hintStyle : deadStyle);
        GUI.Label(new Rect(34, top + 26, 240, 45), Mathf.FloorToInt(scores[lane]).ToString("D3"), scoreStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.64f, 0.72f, 0.86f) } };
        deadStyle = new GUIStyle(hintStyle) { normal = { textColor = new Color(1f, 0.48f, 0.5f) } };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
    }
}

public class Runner : MonoBehaviour
{
    private SplitRunnerGame game;
    private Rigidbody2D body;
    public int Lane { get; private set; }
    private float groundY;

    public void Setup(SplitRunnerGame owner, int lane)
    {
        game = owner;
        Lane = lane;
        body = GetComponent<Rigidbody2D>();
        groundY = lane == 0 ? 1.28f : -2.72f;
    }

    private void Update()
    {
        if (game == null || !game.IsAlive(Lane)) return;
        if ((Lane == 0 && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))) ||
            (Lane == 1 && Input.GetKeyDown(KeyCode.UpArrow))) game.Jump(Lane, body);
    }

    private void FixedUpdate()
    {
        if (game == null || !game.IsAlive(Lane)) return;
        if (body.position.y < groundY) body.position = new Vector2(body.position.x, groundY);
        if (body.velocity.y == 0 && body.position.y <= groundY + 0.03f) body.position = new Vector2(body.position.x, groundY);
    }

    public void ResetRunner()
    {
        body.velocity = Vector2.zero;
        body.position = new Vector2(-5f, Lane == 0 ? 1.28f : -2.72f);
    }
}

public class Obstacle : MonoBehaviour
{
    private SplitRunnerGame game;
    private int lane;
    public void Setup(SplitRunnerGame owner, int laneIndex) { game = owner; lane = laneIndex; }

    private void Update()
    {
        if (game == null || !game.IsAlive(lane)) { Destroy(gameObject); return; }
        transform.position += Vector3.left * game.Speed(lane) * Time.deltaTime;
        if (transform.position.x < -12f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Runner>() != null && game != null) game.Hit(lane);
    }
}

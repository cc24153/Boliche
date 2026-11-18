using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BowlingGame : MonoBehaviour
{
    [SerializeField] private Shader laneShader;
    [SerializeField] private GameObject laneModel;

    private const float HalfLane = BowlingHall.LaneWidth / 2;
    private const float BallRadius = .1085f;
    private const float HeadPin = BowlingHall.HeadPin;
    private const float HookStart = HeadPin * .38f;
    private const float HookPeak = HeadPin * .875f;
    private const float PinSpacing = .3048f;
    private readonly Rigidbody[] pins = new Rigidbody[10];
    private readonly Vector3[] spots = new Vector3[10];
    private readonly bool[] standing = new bool[10];
    private BowlingScore score;
    private BowlingHall hall;
    private BowlingInterface ui;
    private Camera eyes;
    private Rigidbody ball;
    private float position, yaw, pitch, spin, charge, elapsed, stable;
    private bool charging, rolling, gutter, paused, playing, settling;
    private bool shotInputReady;
    private string feedback;

    private void Awake()
    {
        hall = new BowlingHall(transform, laneShader, laneModel);
        eyes = new GameObject("Câmera em primeira pessoa", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        eyes.transform.SetParent(transform);
        eyes.fieldOfView = 54f;
        eyes.nearClipPlane = .035f;
        eyes.farClipPlane = 60;
        eyes.tag = "MainCamera";

        int index = 0;
        for (int row = 0; row < 4; row++)
        for (int column = 0; column <= row; column++)
        {
            spots[index] = new Vector3((column - row * .5f) * PinSpacing, .002f, HeadPin + row * PinSpacing * .8660254f);
            pins[index] = hall.CreatePin(index + 1, spots[index]);
            index++;
        }

        ball = hall.CreateBall();
        ui = new BowlingInterface(this);
        if (IrAoJogo.ConsumePendingStart())
            StartMatch();
        else
            ShowMenu();
    }

    public void StartMatch()
    {
        score = new BowlingScore();
        playing = true;
        paused = false;
        settling = false;
        ResetRack();
        ResetDelivery();
        feedback = "Primeiro arremesso";
        SetCursor(true);
        ui.Play();
        Refresh();
    }

    public void ShowMenu()
    {
        StopAllCoroutines();
        playing = false;
        paused = false;
        settling = false;
        ResetRack();
        ResetDelivery();
        SetCursor(false);
        ui.Menu();
    }

    public void Resume()
    {
        if (!playing || !paused)
            return;
        paused = false;
        shotInputReady = false;
        Time.timeScale = 1f;
        SetCursor(true);
        ui.Play();
    }

    private void Pause()
    {
        paused = true;
        charging = false;
        charge = 0;
        Time.timeScale = 0;
        SetCursor(false);
        ui.Pause();
    }

    private void OnDisable()
    {
        Time.timeScale = 1;
        SetCursor(false);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && playing && !paused)
            Pause();
    }

    private static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void Update()
    {
        if (!playing)
            return;

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool pausePressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
            (mouse != null && mouse.rightButton.wasPressedThisFrame);
        if (pausePressed)
        {
            if (paused) Resume();
            else Pause();
            return;
        }
        if (paused)
            return;

        Vector2 look = mouse == null ? Vector2.zero : mouse.delta.ReadValue();
        yaw = Mathf.Clamp(yaw + look.x * .10f, -32f, 32f);
        pitch = Mathf.Clamp(pitch - look.y * .10f, -35f, 45f);
        if (!rolling && !settling)
        {
            if (keyboard != null)
            {
                float move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                float turn = (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0);
                position = Mathf.Clamp(position + move * .85f * Time.deltaTime, -.40f, .40f);
                spin = Mathf.Clamp(spin + turn * 1.2f * Time.deltaTime, -1, 1);
            }

            PlaceBall();
            bool held = (mouse != null && mouse.leftButton.isPressed) || (keyboard != null && keyboard.spaceKey.isPressed);
            if (!shotInputReady)
            {
                shotInputReady = !held;
            }
            else if (held)
            {
                charging = true;
                charge = Mathf.MoveTowards(charge, 1, Time.deltaTime / 1.25f);
            }
            else if (charging || (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                (keyboard != null && keyboard.spaceKey.wasPressedThisFrame))
            {
                Launch();
            }
        }

        UpdateEyes();
        Refresh();
    }

    private void FixedUpdate()
    {
        if (!rolling)
            return;

        elapsed += Time.fixedDeltaTime;
        Vector3 location = ball.position;
        if (!gutter && Mathf.Abs(location.x) > HalfLane + .015f && location.z > .05f)
            gutter = true;

        if (gutter && ball.gameObject.activeSelf)
        {
            // The channel retains the ball all the way to the pit.
            ball.AddForce(Vector3.right * ((Mathf.Sign(location.x) * .70f - location.x) * 75f
                - ball.linearVelocity.x * 12f), ForceMode.Acceleration);
        }
        else if (!gutter && ball.gameObject.activeSelf && location.z > HookStart && location.z < HeadPin)
        {
            float dry = Mathf.InverseLerp(HookStart, HookPeak, location.z);
            ball.AddForce(Vector3.right * (spin * dry * 1.55f), ForceMode.Acceleration);
        }

        bool reachedPit = location.z >= BowlingHall.BallPit || location.y < -.55f;
        if (reachedPit)
            ball.gameObject.SetActive(false);

        bool still = true;
        for (int i = 0; i < pins.Length; i++)
        {
            if (standing[i] && (pins[i].linearVelocity.sqrMagnitude > .012f ||
                pins[i].angularVelocity.sqrMagnitude > .20f))
                still = false;
        }

        if ((reachedPit || ball.linearVelocity.sqrMagnitude < .06f || elapsed > 7f) && still)
            stable += Time.fixedDeltaTime;
        else
            stable = 0f;

        if (stable >= .65f || elapsed > 10f)
            FinishDelivery();
    }

    private void Launch()
    {
        rolling = true;
        charging = false;
        gutter = false;
        elapsed = stable = 0f;
        feedback = "Bola em jogo";
        Vector3 direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        float speed = Mathf.Lerp(4.5f, 8.7f, charge);
        ball.isKinematic = false;
        ball.linearVelocity = direction * speed;
        ball.angularVelocity = Vector3.Cross(Vector3.up, direction) * (speed / BallRadius);
    }

    private void FinishDelivery()
    {
        rolling = false;
        settling = true;
        ball.gameObject.SetActive(false);
        int knocked = 0;
        int frame = score.Frame;
        int roll = score.Frames[frame].Count;
        for (int i = 0; i < pins.Length; i++)
        {
            if (!standing[i])
                continue;
            Rigidbody pin = pins[i];
            Vector3 offset = pin.position - spots[i];
            bool down = pin.GetComponent<Pin>().IsFallen() || pin.position.y < -.09f ||
                Mathf.Abs(pin.position.x) > HalfLane || pin.position.z >= BowlingHall.LaneLength ||
                new Vector2(offset.x, offset.z).magnitude > .65f;
            standing[i] = !down;
            knocked += down ? 1 : 0;
        }

        bool fullRack = score.Record(knocked);
        string mark = score.Mark(frame, roll);
        feedback = mark == "X" ? "STRIKE!" : mark == "/" ? "SPARE!" :
            knocked == 1 ? "1 pino derrubado" : $"{knocked} pinos derrubados";
        Refresh();
        StartCoroutine(PrepareNext(fullRack));
    }

    private IEnumerator PrepareNext(bool fullRack)
    {
        yield return new WaitForSeconds(1.4f);
        if (score.Finished)
        {
            playing = false;
            SetCursor(false);
            ui.Results(score.Cumulative(9).GetValueOrDefault());
            yield break;
        }

        if (fullRack)
            ResetRack();
        else
        {
            for (int i = 0; i < pins.Length; i++)
                pins[i].gameObject.SetActive(standing[i]);
        }

        settling = false;
        ResetDelivery();
        Refresh();
    }

    private void ResetRack()
    {
        for (int i = 0; i < pins.Length; i++)
        {
            Rigidbody pin = pins[i];
            pin.gameObject.SetActive(true);
            pin.position = spots[i];
            pin.rotation = Quaternion.identity;
            pin.linearVelocity = Vector3.zero;
            pin.angularVelocity = Vector3.zero;
            pin.Sleep();
            standing[i] = true;
        }
    }

    private void ResetDelivery()
    {
        Time.timeScale = 1;
        rolling = charging = shotInputReady = false;
        position = yaw = spin = charge = 0;
        pitch = 11f;
        ball.gameObject.SetActive(true);
        if (!ball.isKinematic)
        {
            ball.linearVelocity = Vector3.zero;
            ball.angularVelocity = Vector3.zero;
        }
        ball.isKinematic = true;
        ball.rotation = Quaternion.identity;
        PlaceBall();
        UpdateEyes();
    }

    private void PlaceBall()
    {
        ball.position = new Vector3(position, BallRadius + .002f, -.48f);
    }

    private void UpdateEyes()
    {
        eyes.transform.SetPositionAndRotation(new Vector3(position, 1.61f, -2.42f),
            Quaternion.Euler(pitch, yaw, 0));
    }

    private void Refresh()
    {
        ui.Refresh(score, feedback, charge, spin, !rolling && !settling);
    }
}

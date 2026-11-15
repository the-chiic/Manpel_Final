using Godot;
using System.Threading.Tasks;
using Attacks;

public partial class CombatScene : Node2D
{
    [Export] public Label MessageLabel;
    [Export] public ProgressBar PlayerHPBar;
    [Export] public ProgressBar PlayerMPBar;
    [Export] public ProgressBar EnemyHPBar;
    [Export] public Label PlayerHPLabel;
    [Export] public Label PlayerMPLabel;
    [Export] public Label EnemyHPLabel;
    [Export] public Container AttackButtons;

    [Export] public Control OutcomePanel;
    [Export] public Label OutcomeTitle;
    [Export] public Button ContinueButton;

    [Export] public Node2D PlayerVisual;
    [Export] public Node2D EnemyVisual;
    [Export] public float JumpHeight = 95f;
    [Export] public float JumpAscentTime = 0.22f;
    [Export] public float JumpHangTime = 0.30f;
    [Export] public float JumpDescentTime = 0.22f;

    private CombatManager _manager;

    // Player Jump-Dodge state
    private bool _canPlayerJump = false;
    private bool _isJumpWindowSweetSpot = false;
    private bool _isPlayerJumping = false;
    private bool _isPlayerAirborne = false;

    // Auto-generated Visual Timing Cue UI above Player
    private Node2D _jumpCueRoot;
    private Label _jumpCueLabel;
    private ColorRect _barBg;
    private ColorRect _barSweetSpot;
    private ColorRect _barFill;
    private const float BarTotalWidth = 110f;
    private const float BarHeight = 12f;

    public override void _Ready()
    {
        _manager = GetNode<CombatManager>("CombatManager");
        _manager.RegisterScene(this);

        _manager.StateChanged += OnStateChanged;
        _manager.HealthUpdated += OnHealthUpdated;
        _manager.MessageUpdated += OnMessageUpdated;

        PlayerVisual ??= GetNodeOrNull<Node2D>("Player")
                         ?? FindChild("Player", true, false) as Node2D;
        EnemyVisual  ??= GetNodeOrNull<Node2D>("EnemyAttackReference")
                         ?? GetNodeOrNull<Node2D>("Enemy")
                         ?? FindChild("Enemy*", true, false) as Node2D;

        CreateJumpTimingIndicator();

        WireButton("SeedButton", PlayerAttacksType.Seed);
        WireButton("FireButton", PlayerAttacksType.Fire);
        WireButton("WaterButton", PlayerAttacksType.Water);
        WireButton("FleshButton", PlayerAttacksType.Flesh);

        ContinueButton.Pressed += () => _manager.CloseCombat();
        OutcomePanel.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_canPlayerJump && !_isPlayerJumping && Input.IsActionJustPressed("ui_accept"))
        {
            if (!_isJumpWindowSweetSpot)
            {
                ShowCueFeedback("TOO EARLY!", new Color(1f, 0.45f, 0.2f));
            }
            else
            {
                ShowCueFeedback("JUMP!", new Color(0.3f, 1f, 0.9f));
            }

            _ = PerformPlayerJumpAsync();
        }
    }

    // ─── Visual Jump Timing Indicator Setup ──────────────────────────────
    private void CreateJumpTimingIndicator()
    {
        _jumpCueRoot = new Node2D
        {
            Name = "JumpTimingIndicator",
            ZIndex = 20,
            Visible = false
        };
        AddChild(_jumpCueRoot);

        // Background of the timing bar
        _barBg = new ColorRect
        {
            Color = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            Position = new Vector2(-BarTotalWidth * 0.5f, 0),
            Size = new Vector2(BarTotalWidth, BarHeight)
        };
        _jumpCueRoot.AddChild(_barBg);

        // Green "Sweet Spot" zone on the right side of the bar
        float sweetSpotWidth = BarTotalWidth * 0.34f;
        _barSweetSpot = new ColorRect
        {
            Color = new Color(0.15f, 0.85f, 0.3f, 0.85f),
            Position = new Vector2((BarTotalWidth * 0.5f) - sweetSpotWidth, 0),
            Size = new Vector2(sweetSpotWidth, BarHeight)
        };
        _jumpCueRoot.AddChild(_barSweetSpot);

        // Moving progress fill bar
        _barFill = new ColorRect
        {
            Color = new Color(1f, 0.85f, 0.2f, 0.95f),
            Position = new Vector2(-BarTotalWidth * 0.5f, 2),
            Size = new Vector2(0, BarHeight - 4)
        };
        _jumpCueRoot.AddChild(_barFill);

        // Floating Prompt Label ("! WAIT..." -> "★ JUMP! [SPACE] ★")
        _jumpCueLabel = new Label
        {
            Text = "! WAIT...",
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-90, -28),
            CustomMinimumSize = new Vector2(180, 24)
        };
        _jumpCueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f));
        _jumpCueLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _jumpCueLabel.AddThemeConstantOverride("outline_size", 5);
        _jumpCueRoot.AddChild(_jumpCueLabel);
    }

    private void ShowCueFeedback(string text, Color color)
    {
        if (_jumpCueLabel == null) return;
        _jumpCueLabel.Text = text;
        _jumpCueLabel.AddThemeColorOverride("font_color", color);
    }

    // ─── Attack Sequences ────────────────────────────────────────────────
    public async Task<bool> PlayAttackSequenceAsync(CombatAction action)
    {
        if (action.IsPlayerAction)
            return await PlayPlayerAttackSequenceAsync(action);
        else
            return await PlayEnemyAttackSequenceAsync(action);
    }

    private async Task<bool> PlayPlayerAttackSequenceAsync(CombatAction action)
    {
        Vector2 playerStart = PlayerVisual != null ? PlayerVisual.Position : Vector2.Zero;
        Vector2 enemyStart  = EnemyVisual != null ? EnemyVisual.Position : Vector2.Zero;

        if (PlayerVisual != null && EnemyVisual != null)
        {
            Vector2 attackSpot = enemyStart + new Vector2(-70f, 0f);
            var lungeTween = CreateTween();
            lungeTween.TweenProperty(PlayerVisual, "position", attackSpot, action.StrikeSeconds)
                      .SetTrans(Tween.TransitionType.Sine)
                      .SetEase(Tween.EaseType.In);

            if (action.EnemyWillJump)
            {
                double jumpDelay = Mathf.Max(0.05, action.StrikeSeconds - JumpAscentTime);
                _ = TriggerDelayedNodeJumpAsync(EnemyVisual, enemyStart, jumpDelay);
            }

            await ToSignal(lungeTween, Tween.SignalName.Finished);
        }
        else
        {
            await ToSignal(GetTree().CreateTimer(action.StrikeSeconds), SceneTreeTimer.SignalName.Timeout);
        }

        bool dodged = action.EnemyWillJump;

        if (PlayerVisual != null)
        {
            var returnTween = CreateTween();
            returnTween.TweenProperty(PlayerVisual, "position", playerStart, 0.35f)
                       .SetTrans(Tween.TransitionType.Quad)
                       .SetEase(Tween.EaseType.Out);
            await ToSignal(returnTween, Tween.SignalName.Finished);
        }

        return dodged;
    }

    private async Task<bool> PlayEnemyAttackSequenceAsync(CombatAction action)
    {
        Vector2 enemyStart  = EnemyVisual != null ? EnemyVisual.Position : Vector2.Zero;
        Vector2 playerStart = PlayerVisual != null ? PlayerVisual.Position : Vector2.Zero;

        // Position the visual timing indicator above the player's head
        if (_jumpCueRoot != null)
        {
            _jumpCueRoot.Position = playerStart + new Vector2(0, -110f);
            _jumpCueRoot.Scale = Vector2.One;
            _barFill.Size = new Vector2(0, BarHeight - 4);
            _barSweetSpot.Visible = action.CanBeJumped;
            _jumpCueRoot.Visible = true;
        }

        _canPlayerJump = action.CanBeJumped;
        _isJumpWindowSweetSpot = false;

        float windup = action.WindupSeconds;
        float strike = action.StrikeSeconds;
        float totalTime = windup + strike;

        // Start filling the timing bar across the full attack duration (windup + strike)
        Tween barTween = null;
        if (_barFill != null)
        {
            float windupWidth = BarTotalWidth * 0.66f;
            barTween = CreateTween();
            barTween.TweenProperty(_barFill, "size:x", windupWidth, windup)
                    .SetTrans(Tween.TransitionType.Linear);
            barTween.TweenProperty(_barFill, "size:x", BarTotalWidth, strike)
                    .SetTrans(Tween.TransitionType.Linear);
        }

        // ─── PHASE 1: Wind-Up (Enemy steps out to center & telegraphs) ───
        if (action.CanBeJumped)
        {
            ShowCueFeedback("! WAIT...", new Color(1f, 0.85f, 0.2f));
            _barFill.Color = new Color(1f, 0.85f, 0.2f, 0.95f);
        }
        else
        {
            ShowCueFeedback("UNJUMPABLE!", new Color(1f, 0.3f, 0.3f));
            _barFill.Color = new Color(0.9f, 0.2f, 0.2f, 0.95f);
        }

        Vector2 windupSpot = enemyStart.Lerp(playerStart + new Vector2(70f, 0f), 0.45f);

        if (EnemyVisual != null)
        {
            var stepOutTween = CreateTween();
            // Step forward for 60% of windup, then shake/flash yellow for 40% of windup
            stepOutTween.TweenProperty(EnemyVisual, "position", windupSpot, windup * 0.6f)
                        .SetTrans(Tween.TransitionType.Quad)
                        .SetEase(Tween.EaseType.Out);
            stepOutTween.TweenProperty(EnemyVisual, "modulate", new Color(1.4f, 1.3f, 0.5f), windup * 0.2f);
            stepOutTween.TweenProperty(EnemyVisual, "modulate", Colors.White, windup * 0.2f);
            await ToSignal(stepOutTween, Tween.SignalName.Finished);
        }
        else
        {
            await ToSignal(GetTree().CreateTimer(windup), SceneTreeTimer.SignalName.Timeout);
        }

        // ─── PHASE 2: Strike & Jump Sweet-Spot Window ("JUMP NOW!") ──────
        _isJumpWindowSweetSpot = true;

        if (action.CanBeJumped && !_isPlayerJumping)
        {
            ShowCueFeedback("★ JUMP! [SPACE] ★", new Color(0.2f, 1f, 0.35f));
            _barFill.Color = new Color(0.2f, 1f, 0.35f, 1f);

            // Pulse the indicator so it catches the player's eye
            var pulseTween = CreateTween();
            pulseTween.TweenProperty(_jumpCueRoot, "scale", new Vector2(1.18f, 1.18f), 0.1f);
            pulseTween.TweenProperty(_jumpCueRoot, "scale", Vector2.One, 0.1f);
        }

        if (EnemyVisual != null && PlayerVisual != null)
        {
            // Flash the enemy bright orange-red as they lunge
            EnemyVisual.Modulate = new Color(1.5f, 0.6f, 0.5f);

            Vector2 strikeSpot = playerStart + new Vector2(55f, 0f);
            var rushTween = CreateTween();
            rushTween.TweenProperty(EnemyVisual, "position", strikeSpot, strike)
                     .SetTrans(Tween.TransitionType.Back)
                     .SetEase(Tween.EaseType.In);

            await ToSignal(rushTween, Tween.SignalName.Finished);
            EnemyVisual.Modulate = Colors.White;
        }
        else
        {
            await ToSignal(GetTree().CreateTimer(strike), SceneTreeTimer.SignalName.Timeout);
        }

        // ─── PHASE 3: Impact Check & Recovery ────────────────────────────
        _canPlayerJump = false;
        _isJumpWindowSweetSpot = false;
        barTween?.Kill();

        bool playerDodged = action.CanBeJumped && _isPlayerAirborne;

        if (playerDodged)
        {
            ShowCueFeedback("DODGED!", new Color(0.2f, 1f, 0.5f));
        }
        else
        {
            ShowCueFeedback("HIT!", new Color(1f, 0.25f, 0.25f));
            if (PlayerVisual != null)
            {
                // Brief red hit-flash on player
                var hitFlash = CreateTween();
                hitFlash.TweenProperty(PlayerVisual, "modulate", new Color(1.6f, 0.4f, 0.4f), 0.12f);
                hitFlash.TweenProperty(PlayerVisual, "modulate", Colors.White, 0.15f);
            }
        }

        // Brief pause at impact before enemy walks back
        await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);

        if (EnemyVisual != null)
        {
            var returnTween = CreateTween();
            returnTween.TweenProperty(EnemyVisual, "position", enemyStart, 0.50f)
                       .SetTrans(Tween.TransitionType.Quad)
                       .SetEase(Tween.EaseType.Out);
            await ToSignal(returnTween, Tween.SignalName.Finished);
        }

        if (_jumpCueRoot != null)
            _jumpCueRoot.Visible = false;

        return playerDodged;
    }

    private async Task TriggerDelayedNodeJumpAsync(Node2D actor, Vector2 groundPos, double delaySeconds)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
        await AnimateJumpArcAsync(actor, groundPos);
    }

    private async Task PerformPlayerJumpAsync()
    {
        _isPlayerJumping = true;
        _isPlayerAirborne = true;

        if (PlayerVisual != null)
        {
            Vector2 groundPos = PlayerVisual.Position;
            await AnimateJumpArcAsync(PlayerVisual, groundPos);
        }
        else
        {
            double totalAirTime = JumpAscentTime + JumpHangTime + JumpDescentTime;
            await ToSignal(GetTree().CreateTimer(totalAirTime), SceneTreeTimer.SignalName.Timeout);
        }

        _isPlayerAirborne = false;
        _isPlayerJumping = false;
    }

    private async Task AnimateJumpArcAsync(Node2D actor, Vector2 groundPos)
    {
        if (!IsInstanceValid(actor)) return;

        Vector2 peakPos = groundPos + new Vector2(0f, -JumpHeight);

        var upTween = CreateTween();
        upTween.TweenProperty(actor, "position", peakPos, JumpAscentTime)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.Out);
        await ToSignal(upTween, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(JumpHangTime), SceneTreeTimer.SignalName.Timeout);

        var downTween = CreateTween();
        downTween.TweenProperty(actor, "position", groundPos, JumpDescentTime)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.In);
        await ToSignal(downTween, Tween.SignalName.Finished);
    }

    private void WireButton(string nodeName, PlayerAttacksType type)
    {
        if (AttackButtons == null) return;

        var btn = AttackButtons.GetNodeOrNull<Button>(nodeName);
        if (btn != null)
        {
            btn.Pressed += () =>
            {
                var action = new CombatAction
                {
                    IsPlayerAction = true,
                    AttackName = type.ToString(),
                    PlayerAttack = type
                };
                _manager.ExecuteAction(action);
            };
        }
    }

    private void OnStateChanged(int state)
    {
        var combatState = (CombatState)state;
        SetButtonsEnabled(combatState == CombatState.PlayerTurn);

        if (combatState == CombatState.Victory || combatState == CombatState.Defeat)
        {
            OutcomeTitle.Text = combatState == CombatState.Victory ? "VICTORY!" : "DEFEAT...";
            OutcomePanel.Visible = true;
            ContinueButton.CallDeferred("grab_focus");
        }
    }

    private void OnHealthUpdated(int p, int pMax, int pMp, int pMaxMp, int e, int eMax)
    {
        PlayerHPBar.MaxValue = pMax;
        PlayerHPBar.Value = p;

        if (PlayerMPBar != null)
        {
            PlayerMPBar.MaxValue = pMaxMp;
            PlayerMPBar.Value = pMp;
            PlayerMPLabel.Text = $"MP: {pMp} / {pMaxMp}";
        }

        EnemyHPBar.MaxValue = eMax;
        EnemyHPBar.Value = e;

        PlayerHPLabel.Text = $"HP: {p} / {pMax}";
        EnemyHPLabel.Text = $"Enemy: {e} / {eMax}";
    }

    private void OnMessageUpdated(string msg) => MessageLabel.Text = msg;

    private void SetButtonsEnabled(bool enabled)
    {
        if (AttackButtons == null) return;

        Button firstButton = null;
        foreach (var c in AttackButtons.GetChildren())
        {
            if (c is Button b)
            {
                b.Disabled = !enabled;
                if (enabled && firstButton == null) firstButton = b;
            }
        }

        if (enabled && firstButton != null)
            firstButton.CallDeferred("grab_focus");
    }
}
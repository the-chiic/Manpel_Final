using Godot;
using Attacks;
using System.Threading.Tasks;

public partial class BattleScene : Node2D
{
    private enum BattleState { PlayerTurn, Busy, Victory, Defeat }

    [Export] public Label       MessageLabel;
    [Export] public ProgressBar PlayerHPBar;
    [Export] public ProgressBar EnemyHPBar;
    [Export] public Label       PlayerHPLabel;
    [Export] public Label       EnemyHPLabel;
    [Export] public Container   AttackButtons;

    [Export] public Control OutcomePanel;
    [Export] public Label   OutcomeTitle;
    [Export] public Button  ContinueButton;

    private BattleState _state = BattleState.Busy;
    private PlayerState _playerState;
    private EnemyDefinitionData _enemyDef;
    private Enemy1 _mapEnemy;
    private Player _playerNode;
    private Node2D _mapScene;

    private int _currentPlayerHp;
    private int _currentEnemyHp;

    private Node _playerOriginalParent;
    private Vector2 _playerOriginalPos;
    private Node _enemyOriginalParent;
    private Vector2 _enemyOriginalPos;

    public void Initialize(Player playerNode, Enemy1 mapEnemy, Node2D mapScene)
    {
        _playerNode = playerNode;
        _playerState = playerNode.State ?? new PlayerState(playerNode.Definition);
        _playerNode.State = _playerState;

        _mapEnemy = mapEnemy;
        _enemyDef = mapEnemy.enemyData;
        _mapScene = mapScene;

        // Capture BEFORE BattleTransition reparents them.
        _playerOriginalParent = playerNode.GetParent();
        _playerOriginalPos    = playerNode.Position;
        _enemyOriginalParent  = mapEnemy.GetParent();
        _enemyOriginalPos     = mapEnemy.Position;

        _currentPlayerHp = _playerState.CurrentHP;
        _currentEnemyHp  = _enemyDef.MaxHP;
    }

    public override void _Ready()
    {
        GD.Print($"Battle vs {_enemyDef.DisplayName} ({_currentEnemyHp} HP)");

        WireAttackButton("SeedButton",  PlayerAttacksType.Seed);
        WireAttackButton("FireButton",  PlayerAttacksType.Fire);
        WireAttackButton("WaterButton", PlayerAttacksType.Water);
        WireAttackButton("FleshButton", PlayerAttacksType.Flesh);

        RefreshHud();
        StartPlayerTurn();
    }

    // ─── Player turn ─────────────────────────────────────────────────────

    private void StartPlayerTurn()
    {
        _state = BattleState.PlayerTurn;
        SetButtonsEnabled(true);
        SetMessage("What will you do?");
        RefreshHud();
    }

    public async void OnPlayerAttackSelected(PlayerAttacksType attackType)
    {
        if (_state != BattleState.PlayerTurn) return;
        _state = BattleState.Busy;
        SetButtonsEnabled(false);

        int damage = CalculatePlayerDamage(attackType);
        _currentEnemyHp = Mathf.Max(0, _currentEnemyHp - damage);
        SetMessage($"Player used {attackType} for {damage} damage!");
        RefreshHud();

        await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);

        if (_currentEnemyHp <= 0)
        {
            _state = BattleState.Victory;
            SetMessage($"{_enemyDef.DisplayName} was defeated!");
            await ShowOutcome(true);
            EndBattle(true);
            return;
        }

        await ExecuteEnemyTurn();
    }

    // ─── Enemy turn ──────────────────────────────────────────────────────
    private async Task ExecuteEnemyTurn()
    {
        _state = BattleState.Busy;
        SetButtonsEnabled(false);

        EnemyAttacksType enemyAttack = PickEnemyAttack();
        int damage = CalculateEnemyDamage(enemyAttack);
        _currentPlayerHp = Mathf.Max(0, _currentPlayerHp - damage);

        SetMessage($"{_enemyDef.DisplayName} used {enemyAttack} for {damage} damage!");
        RefreshHud();

        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

        if (_currentPlayerHp <= 0)
        {
            _state = BattleState.Defeat;
            SetMessage("You were defeated...");
            await ShowOutcome(false);
            EndBattle(false);
            return;
        }

        StartPlayerTurn();
    }

    private EnemyAttacksType PickEnemyAttack()
    {
        var options = new[] {
            EnemyAttacksType.Fire,
            EnemyAttacksType.Water,
            EnemyAttacksType.Seed
        };
        return options[GD.RandRange(0, options.Length - 1)];
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    private void WireAttackButton(string nodeName, PlayerAttacksType type)
    {
        var btn = GetNodeOrNull<Button>($"UI/AttackButtons/{nodeName}")
                  ?? FindChild(nodeName, true, false) as Button;
        if (btn != null)
            btn.Pressed += () => OnPlayerAttackSelected(type);
    }

    private void RefreshHud()
    {
        if (PlayerHPBar != null)
        {
            PlayerHPBar.MaxValue = _playerState.Definition.MaxHP;
            PlayerHPBar.Value    = _currentPlayerHp;
        }
        if (EnemyHPBar != null)
        {
            EnemyHPBar.MaxValue = _enemyDef.MaxHP;
            EnemyHPBar.Value    = _currentEnemyHp;
        }
        if (PlayerHPLabel != null)
            PlayerHPLabel.Text = $"HP: {_currentPlayerHp}/{_playerState.Definition.MaxHP}";
        if (EnemyHPLabel != null)
            EnemyHPLabel.Text  = $"{_enemyDef.DisplayName}: {_currentEnemyHp}/{_enemyDef.MaxHP}";
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (AttackButtons == null) return;
        Button first = null;
        foreach (var c in AttackButtons.GetChildren())
        {
            if (c is Button b)
            {
                b.Disabled = !enabled;
                if (enabled && first == null) first = b;
            }
        }
        if (enabled && first != null) first.GrabFocus();
    }

    private void SetMessage(string text)
    {
        if (MessageLabel != null) MessageLabel.Text = text;
        GD.Print(text);
    }

    private int CalculatePlayerDamage(PlayerAttacksType attack) => attack switch
    {
        PlayerAttacksType.Seed  => 5,
        PlayerAttacksType.Fire  => 12,
        PlayerAttacksType.Water => 10,
        PlayerAttacksType.Flesh => 15,
        _ => 1
    };

    private int CalculateEnemyDamage(EnemyAttacksType attack) => attack switch
    {
        EnemyAttacksType.Fire      => 8,
        EnemyAttacksType.Water     => 6,
        EnemyAttacksType.Seed      => 4,
        EnemyAttacksType.Explosion => 20,
        _ => 2
    };

    // ─── Exit ────────────────────────────────────────────────────────────
    public void EndBattle(bool playerWon)
    {
        _playerState.CurrentHP = _currentPlayerHp;

        // Player always comes back to the overworld.
        if (IsInstanceValid(_playerNode))
        {
            _playerNode.Reparent(_playerOriginalParent, false);
            _playerNode.Position = _playerOriginalPos;
            _playerNode.SetPhysicsProcess(true);
        }

        // Enemy: free on victory, return on defeat.
        if (IsInstanceValid(_mapEnemy))
        {
            if (playerWon)
                _mapEnemy.QueueFree();
            else
            {
                _mapEnemy.Reparent(_enemyOriginalParent, false);
                _mapEnemy.Position = _enemyOriginalPos;
                _mapEnemy.SetPhysicsProcess(true);
            }
        }

        _mapScene.ProcessMode = Node.ProcessModeEnum.Inherit;
        _mapScene.Visible = true;
        GetTree().CurrentScene = _mapScene;

        TurnManager.Instance?.ResetToPlayerTurn();
        BattleTransition.BattleEnd(_playerNode);   // CanMove = true
        QueueFree(); 
    }

    private async Task<bool> ShowOutcome(bool playerWon)
    {
        OutcomeTitle.Text = playerWon ? "Victory!" : "Defeat...";
        OutcomePanel.Visible = true;
        ContinueButton.GrabFocus();
        await ToSignal(ContinueButton, Button.SignalName.Pressed);
        OutcomePanel.Visible = false;
        return true;
    }
}
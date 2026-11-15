using System.Collections.Generic;
using Attacks;

public enum CombatState { PlayerTurn, EnemyTurn, Busy, Victory, Defeat }

public class AttackProfile
{
    public string DisplayName { get; set; }
    public int BaseDamage { get; set; }
    public bool CanBeJumped { get; set; } = true;
    public float BaseEnemyJumpChance { get; set; } = 0.15f;

    // WindupSeconds: Enemy steps out & charges (jumping here is too early!)
    public float WindupSeconds { get; set; } = 0.90f;
    // StrikeSeconds: The "JUMP NOW!" window as the enemy lunges
    public float StrikeSeconds { get; set; } = 0.55f;
}

public static class CombatDatabase
{
    public static readonly Dictionary<PlayerAttacksType, AttackProfile> PlayerAttacks = new()
    {
        [PlayerAttacksType.Seed] = new AttackProfile
        {
            DisplayName = "Seed",
            BaseDamage = 5,
            CanBeJumped = true,
            BaseEnemyJumpChance = 0.25f,
            StrikeSeconds = 0.55f
        },
        [PlayerAttacksType.Fire] = new AttackProfile
        {
            DisplayName = "Fire",
            BaseDamage = 12,
            CanBeJumped = true,
            BaseEnemyJumpChance = 0.15f,
            StrikeSeconds = 0.60f
        },
        [PlayerAttacksType.Water] = new AttackProfile
        {
            DisplayName = "Water",
            BaseDamage = 10,
            CanBeJumped = true,
            BaseEnemyJumpChance = 0.20f,
            StrikeSeconds = 0.60f
        },
    };

    public static readonly Dictionary<EnemyAttacksType, AttackProfile> EnemyAttacks = new()
    {
        [EnemyAttacksType.Seed] = new AttackProfile
        {
            DisplayName = "Seed",
            BaseDamage = 4,
            CanBeJumped = true,
            WindupSeconds = 0.95f,
            StrikeSeconds = 0.55f
        },
        [EnemyAttacksType.Fire] = new AttackProfile
        {
            DisplayName = "Fire",
            BaseDamage = 8,
            CanBeJumped = true,
            WindupSeconds = 1.25f,
            StrikeSeconds = 0.50f
        },
        [EnemyAttacksType.Explosion] = new AttackProfile
        {
            DisplayName = "Explosion",
            BaseDamage = 20,
            CanBeJumped = false,
            WindupSeconds = 1.20f,
            StrikeSeconds = 0.60f
        }
    };

    public static AttackProfile GetPlayerAttack(PlayerAttacksType type) =>
        PlayerAttacks.TryGetValue(type, out var profile)
            ? profile
            : new AttackProfile { DisplayName = type.ToString(), BaseDamage = 1 };

    public static AttackProfile GetEnemyAttack(EnemyAttacksType type) =>
        EnemyAttacks.TryGetValue(type, out var profile)
            ? profile
            : new AttackProfile { DisplayName = type.ToString(), BaseDamage = 2 };
}

public class CombatAction
{
    public string AttackName { get; set; }
    public int Damage { get; set; }
    public bool IsPlayerAction { get; set; }
    public PlayerAttacksType PlayerAttack { get; set; }
    public EnemyAttacksType EnemyAttack { get; set; }

    public bool CanBeJumped { get; set; } = true;
    public float WindupSeconds { get; set; } = 1.0f;
    public float StrikeSeconds { get; set; } = 0.55f;
    public bool EnemyWillJump { get; set; }
}
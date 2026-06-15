namespace Game.Boss
{
    /// <summary>Boss2 动画状态</summary>
    public enum BossState
    {
        Idle = 0,
        MeleeAttack = 1,        // 近战
        Casting = 2,            // 施法引导 → 触发 Spell
        Spell = 3,              // 法术攻击
        CreateShield = 4,       // 创造护盾
        ShieldHit = 5,          // 护盾受击
        TeleportStart = 6,      // 传送消失
        TeleportEnd = 7,        // 传送出现
        Hurt = 8,               // 受伤硬直
        // P2 解锁
        Explode = 9,
        Summon = 10,
        CastSpell = 11,
        Death = 12
    }
}

namespace Game.Boss
{
    /// <summary>
    /// Boss 动画状态。由 BossStateMachine.SetState() → Animator "State" 参数驱动。
    /// </summary>
    public enum BossState
    {
        Idle = 0,
        Move = 1,
        Telegraph = 2,          // 攻击前摇闪烁
        MeleeAttack = 3,        // 近战
        Spell = 4,              // 法术攻击（由 Casting 后触发）
        Casting = 5,            // 施法引导（Spell 的前置）
        CreateShield = 6,       // 创造护盾
        TeleportStart = 7,      // 传送消失
        TeleportEnd = 8,        // 传送出现
        ShieldHit = 9,          // 护盾被击中
        Hurt = 10,              // 受伤硬直
        Death = 11,             // 死亡
        Rest = 12               // 攻击后休息窗口
    }
}

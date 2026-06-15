using System;

namespace Game.Boss
{
    [Serializable]
    public enum BossPhase
    {
        /// <summary>P1: 100%-50% HP — 近战+法术混合</summary>
        Phase1 = 1,
        /// <summary>P2: 50%-0% HP — 新攻击解锁，更快节奏</summary>
        Phase2 = 2
    }
}

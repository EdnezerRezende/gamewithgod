using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valentes
{
    /// <summary>Quem o golpe atual alcança: a estocada da lança atravessa até dois em linha; a varredura acerta todos em volta.</summary>
    public static class SpearHits
    {
        public static List<T> Pick<T>(BenaiaArms arms, IEnumerable<T> items, Func<T, Vector3> pos)
        {
            List<T> list = new List<T>();
            foreach (T i in items) if (arms.InReach(pos(i))) list.Add(i);
            if (arms.weapon == BenaiaWeapon.Spear && !arms.Sweeping && list.Count > 2)
            {
                Vector3 p = arms.player.Position;
                list.Sort((a, b) => Vector3.Distance(pos(a), p).CompareTo(Vector3.Distance(pos(b), p)));
                list.RemoveRange(2, list.Count - 2);
            }
            return list;
        }
    }
}

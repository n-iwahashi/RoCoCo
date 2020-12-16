using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TaskDynamics
{
    private static Vector2 _CalConstrainedPos(float tl, Vector2 pos0, Vector2 pos1)
    {
        Vector2 v = pos1 - pos0;
        float d = v.magnitude;
        return pos0 + tl / d * v;
    }

    public static Vector2[] TwoAgentControl2(float tl,
        Vector2 a0_pos, Vector2 a0_vel, float a0_weight,
        Vector2 a1_pos, Vector2 a1_vel, float a1_weight, float velocity_control_interval)
    {
        Vector2 v = a1_pos - a0_pos;
        Vector2 v2 = v / 2.0f;
        Vector2 center = a0_pos + v2;
        float d = v.magnitude;
        Vector2 normalized_v2 = v2 * tl / d;
        Vector2 a0_pos_t = center - normalized_v2;
        Vector2 a1_pos_t = center + normalized_v2;

        Vector2 a0_vel_t = a0_vel * velocity_control_interval;
        Vector2 a1_vel_t = a1_vel * velocity_control_interval;

        Vector2 constrained_next_a0_pos = _CalConstrainedPos(tl, a1_pos_t + a1_vel_t, a0_pos_t + a0_vel_t);
        Vector2 constrained_a0_vel = constrained_next_a0_pos - a0_pos_t;
        Vector2 real_a0_vel = a0_weight * a0_vel_t + a1_weight * constrained_a0_vel;

        Vector2 constrained_next_a1_pos = _CalConstrainedPos(tl, a0_pos_t + a0_vel_t, a1_pos_t + a1_vel_t);
        Vector2 constrained_a1_vel = constrained_next_a1_pos - a1_pos_t;
        Vector2 real_a1_vel = a0_weight * constrained_a1_vel + a1_weight * a1_vel_t;

        real_a0_vel /= velocity_control_interval;
        real_a1_vel /= velocity_control_interval;

        return new Vector2[2] { real_a0_vel, real_a1_vel };
    }

    public static int GetStartIndex(List<Vector2> agent1Path, List<Vector2> agent2Path, Vector2 a1_pos, Vector2 a2_pos)
    {
        int count = agent1Path.Count;
        if (count > 50) count = 50;

        float distance = 10000;
        int nearest_index = 0;
        for (int index = 0; index < count; index++)
        {
            float dist_tmp = (agent1Path[index] - a1_pos).magnitude + (agent2Path[index] - a2_pos).magnitude;
            // 同じ距離なら先の位置を優先する。折り返すような軌道を防ぐ？
            if (dist_tmp <= distance)
            {
                distance = dist_tmp;
                nearest_index = index;
            }
        }
        return nearest_index;
    }
}

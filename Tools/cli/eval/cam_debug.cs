var sb = new System.Text.StringBuilder();
foreach (var fc in UnityEngine.Object.FindObjectsByType<GliderJam.Flight.FollowCamera>(UnityEngine.FindObjectsSortMode.None))
{
    var g = fc.target;
    var local = g.transform.InverseTransformPoint(fc.transform.position);
    sb.Append(fc.name).Append(" dist=").Append(UnityEngine.Vector3.Distance(fc.transform.position, g.transform.position).ToString("F1"))
      .Append(" local=").Append(local.ToString("F1")).Append(" fov=").Append(fc.GetComponent<UnityEngine.Camera>().fieldOfView.ToString("F0"))
      .Append(" crashed=").Append(g.Crashed).Append(" speed=").Append(g.Speed.ToString("F0")).Append(" | ");
}
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>(UnityEngine.FindObjectsSortMode.None))
    if (s.name == "Speed Streaks") sb.Append("streaks alive=").Append(s.particleCount).Append(" layer=").Append(s.gameObject.layer).Append(" ");
return sb.ToString();

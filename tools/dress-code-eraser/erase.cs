using System;
using System.Diagnostics;
using System.IO;

class P {
  const int W = 1080, H = 1920, FS = W * H * 3;
  // dress-code rect relative to card (in final-frame pixels), and final card width
  static double RX0 = 176, RX1 = 858, RY0 = 1106, RY1 = 1231, CW = 1017;

  static bool White(byte[] f, int x, int y) {
    int i = (y * W + x) * 3;
    return f[i] > 244 && f[i + 1] > 244 && f[i + 2] > 244;
  }

  static void ReadFull(Stream s, byte[] b) {
    int o = 0;
    while (o < b.Length) { int n = s.Read(b, o, b.Length - o); if (n <= 0) throw new EndOfStreamException(); o += n; }
  }

  // card bbox: columns/rows with many white pixels
  static void Run(byte[] f, int y, out int s, out int e) {
    s = -1; e = -1; int bs = -1, be = -1, cs = -1, last = -100;
    for (int x = 0; x < W; x++) {
      if (!White(f, x, y)) continue;
      if (x - last > 20) cs = x;
      last = x;
      if (last - cs > be - bs) { bs = cs; be = last; }
    }
    s = bs; e = be;
  }

  static bool Bbox(byte[] f, out int l, out int r, out int t) {
    l = r = t = -1;
    int s, e;
    for (int y = 0; y < H - 40; y++) {
      Run(f, y, out s, out e);
      if (e - s < 150) continue;
      bool ok = true;
      for (int k = 3; k <= 30 && ok; k += 3) { int s2, e2; Run(f, y + k, out s2, out e2); if (e2 - s2 < 150) ok = false; }
      if (!ok) continue;
      t = y;
      var ls = new System.Collections.Generic.List<int>(); var rs = new System.Collections.Generic.List<int>();
      for (int k = 4; k <= 24; k += 2) { int s2, e2; Run(f, y + k, out s2, out e2); ls.Add(s2); rs.Add(e2); }
      ls.Sort(); rs.Sort();
      l = ls[ls.Count / 2]; r = rs[rs.Count / 2];
      return true;
    }
    return false;
  }

  static bool BboxOld(byte[] f, out int l, out int r, out int t) {
    int[] col = new int[W];
    for (int y = 0; y < H; y += 2) for (int x = 0; x < W; x++) if (White(f, x, y)) col[x]++;
    l = -1; r = -1; t = -1;
    int best = 0, cs = -1;
    for (int x = 0; x <= W; x++) {
      bool on = x < W && col[x] > 60;
      if (on && cs < 0) cs = x;
      if (!on && cs >= 0) { if (x - cs > best) { best = x - cs; l = cs; r = x - 1; } cs = -1; }
    }
    if (best < 150) return false;
    // top: first row where many white pixels inside [l,r]
    int need = (r - l) / 2;
    for (int y = 0; y < H; y++) {
      int c = 0;
      for (int x = l; x <= r; x += 2) if (White(f, x, y)) c++;
      if (c * 2 > need) { t = y; return true; }
    }
    return false;
  }

  // navy envelope pixel (saturated blue), as opposed to black text / grey card
  static bool Env(byte[] f, int i) { return f[i + 2] - f[i] > 25 && f[i + 2] < 210; }

  static void Propagate(double[,] v, bool[] ok, int n) {
    int last = -1;
    for (int j = 0; j < n; j++) { if (ok[j]) last = j; else if (last >= 0) for (int c = 0; c < 3; c++) v[j, c] = v[last, c]; }
    last = -1;
    for (int j = n - 1; j >= 0; j--) {
      if (ok[j]) last = j;
      else if (last >= 0) {
        // fill from right if no left neighbour, or nearer
        int lft = -1; for (int q = j - 1; q >= 0; q--) if (ok[q]) { lft = q; break; }
        if (lft < 0 || last - j < j - lft) for (int c = 0; c < 3; c++) v[j, c] = v[last, c];
      }
    }
  }

  static int Luma(byte[] f, int x, int y) { int i = (y * W + x) * 3; return (f[i] * 3 + f[i + 1] * 6 + f[i + 2]) / 10; }

  static void Main(string[] a) {
    string inp = a[0], outp = a[1], log = a[2];
    bool debug = a.Length > 3;
    var dec = new Process();
    dec.StartInfo = new ProcessStartInfo("ffmpeg", "-v error -i \"" + inp + "\" -f rawvideo -pix_fmt rgb24 -") { UseShellExecute = false, RedirectStandardOutput = true };
    dec.Start();
    Process enc = null; Stream eo = null;
    if (!debug) {
      enc = new Process();
      enc.StartInfo = new ProcessStartInfo("ffmpeg", "-v error -y -f rawvideo -pix_fmt rgb24 -s 1080x1920 -r 60 -i - -i \"" + inp + "\" -map 0:v -map 1:a -c:v libx264 -crf 16 -preset slow -pix_fmt yuv420p -c:a copy -movflags +faststart \"" + outp + "\"") { UseShellExecute = false, RedirectStandardInput = true };
      enc.Start(); eo = enc.StandardInput.BaseStream;
    }
    var ds = dec.StandardOutput.BaseStream;
    var lg = new StreamWriter(log);
    byte[] f = new byte[FS];
    int n = 0;
    while (true) {
      try { ReadFull(ds, f); } catch (EndOfStreamException) { break; }
      int l, r, t;
      if (Bbox(f, out l, out r, out t)) {
        double s = (r - l + 1) / CW;
        int x0 = (int)Math.Round(l + RX0 * s), x1 = (int)Math.Round(l + RX1 * s);
        int y0 = (int)Math.Round(t + RY0 * s), y1 = (int)Math.Round(t + RY1 * s);
        lg.WriteLine("{0} l={1} r={2} t={3} rect={4},{5}-{6},{7}", n, l, r, t, x0, y0, x1, y1);
        if (y0 > 10 && y1 < H - 10 && x0 > 20 && x1 < W - 20) {
          // smoothed reference rows above and below the rect
          int nW = x1 - x0 + 1, R = 14;
          double[,] top = new double[nW, 3], bot = new double[nW, 3];
          bool[] tv = new bool[nW], bv = new bool[nW];
          for (int x = x0; x <= x1; x++) {
            double[] st = new double[3], sb = new double[3]; int ct = 0, cb = 0;
            for (int dx = -R; dx <= R; dx++) for (int dy = 2; dy <= 6; dy++) {
              int xx = Math.Max(0, Math.Min(W - 1, x + dx));
              int it = ((y0 - dy) * W + xx) * 3, ib = ((y1 + dy) * W + xx) * 3;
              if (!Env(f, it)) { for (int c = 0; c < 3; c++) st[c] += f[it + c]; ct++; }
              if (!Env(f, ib)) { for (int c = 0; c < 3; c++) sb[c] += f[ib + c]; cb++; }
            }
            int j = x - x0;
            tv[j] = ct > 20; bv[j] = cb > 40;
            for (int c = 0; c < 3; c++) { top[j, c] = ct > 0 ? st[c] / ct : 0; bot[j, c] = cb > 0 ? sb[c] / cb : 0; }
          }
          Propagate(top, tv, nW); Propagate(bot, bv, nW);
          bool anyB = Array.IndexOf(bv, true) >= 0;
          for (int x = x0; x <= x1; x++) {
            int edge = Math.Min(x - x0, x1 - x); double fe = Math.Min(1.0, (edge + 1) / 8.0);
            for (int y = y0; y <= y1; y++) {
              double k = anyB ? (double)(y - y0 + 1) / (y1 - y0 + 2) : 0;
              int i = (y * W + x) * 3;
              if (Env(f, i)) continue;
              for (int c = 0; c < 3; c++) {
                double v = top[x - x0, c] * (1 - k) + bot[x - x0, c] * k;
                f[i + c] = (byte)Math.Round(v * fe + f[i + c] * (1 - fe));
              }
            }
          }
        }
      } else lg.WriteLine("{0} none", n);
      if (!debug) eo.Write(f, 0, FS);
      n++;
    }
    lg.Close();
    if (!debug) { eo.Close(); enc.WaitForExit(); }
    dec.WaitForExit();
    Console.WriteLine("frames " + n);
  }
}

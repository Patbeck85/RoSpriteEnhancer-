using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RoSpriteEnhancer.Core.Scaling
{
    /// <summary>
    /// xBRZ-inspirierter Pixel-Art-Scaler (eigene Implementierung nach dem
    /// von Zenju veröffentlichten Konzept, kein 1:1-Port des Original-C++-Codes).
    ///
    /// Warum das besser ist als Scale2x (EPX):
    ///
    ///   Scale2x analysiert nur 4 direkte Nachbarn (N/S/E/W) →
    ///   erkennt nur perfekt horizontale/vertikale Kanten.
    ///
    ///   Dieser Algorithmus analysiert ein erweitertes Fenster (3×3 plus
    ///   Eckpunkte 2 Schritte diagonal) mit Sub-Pixel-Blending → erkennt
    ///   Diagonalen, Kurven, 45°-Kanten, und interpoliert sie korrekt.
    ///
    ///   Beispiel (Diagonale):
    ///   Scale2x:  ██░░    xBRZ:  ██▓░
    ///             ░░██           ░▓██
    ///   Scale2x: harte Treppchen, xBRZ: glatte Diagonale.
    ///
    /// ECKEN-BESTÄTIGUNG (rundere Helme/Schilde): Für jede der 4 Sub-Pixel-
    /// Ecken wird zusätzlich der Pixel 2 Schritte diagonal weiter außen
    /// geprüft. Setzt sich die Farbe dort fort, handelt es sich um eine
    /// echte durchgehende Kurve (großflächige runde Form) → stärkeres
    /// Blending (87.5%) für eine sichtbar rundere Silhouette. Bestätigt sich
    /// die Kurve dort NICHT, bleibt es beim konservativeren 75%-Blending,
    /// damit dünne Diagonal-Linien (Waffen, Cape-Kanten) nicht künstlich
    /// aufgebläht wirken.
    ///
    /// Unterstützte Faktoren: 2x, 4x (4x = zweimal 2x)
    /// CPU-only, kein GPU nötig.
    /// </summary>
    public static class XbrzScaler
    {
        // ─── ÖFFENTLICHE API ─────────────────────────────────────────────────────

        public static Bitmap Scale2x(Bitmap src) => ScaleStep(src);

        public static Bitmap Scale4x(Bitmap src)
        {
            using Bitmap step1 = ScaleStep(src);
            return ScaleStep(step1);
        }

        // ─── KERN-ALGORITHMUS ────────────────────────────────────────────────────

        private static Bitmap ScaleStep(Bitmap src)
        {
            int sw = src.Width;
            int sh = src.Height;

            BitmapData srcData = src.LockBits(new Rectangle(0, 0, sw, sh),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int[] s = new int[sw * sh];
            try   { Marshal.Copy(srcData.Scan0, s, 0, s.Length); }
            finally { src.UnlockBits(srcData); }

            int dw = sw * 2;
            int dh = sh * 2;
            int[] d = new int[dw * dh];

            for (int y = 0; y < sh; y++)
            {
                // Zeilen mit Clamp-Behandlung vorladen
                int y0  = Math.Max(y - 1, 0);
                int y2  = Math.Min(y + 1, sh - 1);
                // Zusätzliche "weit entfernte" Zeilen für die Ecken-Bestätigung
                // (2 Schritte statt nur 1) — das ist der Schlüssel zur echten
                // Rundung: bei runden Formen (Helme, Schilde) setzt sich die
                // Kurve auch 2 Pixel weiter fort, bei dünnen Diagonal-Linien
                // (Waffen, Cape-Kanten) meist nicht.
                int yFar0 = Math.Max(y - 2, 0);
                int yFar2 = Math.Min(y + 2, sh - 1);

                for (int x = 0; x < sw; x++)
                {
                    int x0 = Math.Max(x - 1, 0);
                    int x2 = Math.Min(x + 1, sw - 1);
                    int x3 = Math.Min(x + 2, sw - 1);
                    int xFar0 = Math.Max(x - 2, 0);

                    // 3×3 Nachbarschaft + erweiterter Rand:
                    //  A B C
                    //  E F G H
                    //  I J K
                    //
                    // F = aktueller Pixel (x,y)
                    // H = rechts außen (für Diagonal-Gewichte)
                    int A = s[y0*sw+x0]; int B = s[y0*sw+x];  int C = s[y0*sw+x2];
                    int E = s[y*sw+x0];  int F = s[y*sw+x];   int G = s[y*sw+x2];  int H = s[y*sw+x3];
                    int I = s[y2*sw+x0]; int J = s[y2*sw+x];  int K = s[y2*sw+x2];

                    // Weit entfernte Diagonal-Pixel (2 Schritte) für jede der
                    // 4 Ecken-Richtungen — bestätigen ob dort tatsächlich eine
                    // durchgehende Kurve verläuft (→ stärker runden) oder nicht
                    // (→ konservativ bleiben, kein Über-Runden dünner Linien).
                    int farNW = s[yFar0*sw + xFar0];
                    int farNE = s[yFar0*sw + x3];
                    int farSW = s[yFar2*sw + xFar0];
                    int farSE = s[yFar2*sw + x3];

                    // Ausgabe-Positionen für die 4 Sub-Pixel
                    int dx = x * 2;
                    int dy = y * 2;

                    // xBRZ 2x Kern
                    // Analyse der 8 Nachbarn von F (Kernelement)
                    // B E F G H J
                    //
                    // Kanten-Erkennung durch Farbdistanz
                    bool eq_BF = Eq(B, F); bool eq_FH = Eq(F, H);
                    bool eq_EF = Eq(E, F); bool eq_FJ = Eq(F, J);

                    bool eq_BE = Eq(B, E); bool eq_BG = Eq(B, G);
                    bool eq_EI = Eq(E, I); bool eq_GK = Eq(G, K);

                    // Diagonal-Gewichte: Wie stark ist jede Ecke diagonal?
                    float wDiagA = DiagWeight(E, F, B, G, J, H, I, K);
                    float wDiagB = DiagWeight(H, F, B, G, J, E, K, I);
                    float wDiagC = DiagWeight(J, F, B, G, E, H, I, K);
                    float wDiagD = DiagWeight(E, J, G, I, B, K, H, A);

                    // Sub-Pixel oben-links (F0)
                    d[dy * dw + dx] = BlendPixel(F, B, E, G,
                        eq_BF, eq_EF, eq_BE, eq_EI, wDiagA, farNW);

                    // Sub-Pixel oben-rechts (F1)
                    d[dy * dw + dx + 1] = BlendPixel(F, B, G, E,
                        eq_BF, eq_FH, eq_BG, eq_GK, wDiagB, farNE);

                    // Sub-Pixel unten-links (F2)
                    d[(dy+1) * dw + dx] = BlendPixel(F, J, E, G,
                        eq_FJ, eq_EF, eq_EI, eq_BF, wDiagC, farSW);

                    // Sub-Pixel unten-rechts (F3)
                    d[(dy+1) * dw + dx + 1] = BlendPixel(F, J, G, E,
                        eq_FJ, eq_FH, eq_GK, eq_BF, wDiagD, farSE);
                }
            }

            Bitmap dst = new Bitmap(dw, dh, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(new Rectangle(0, 0, dw, dh),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try   { Marshal.Copy(d, 0, dstData.Scan0, d.Length); }
            finally { dst.UnlockBits(dstData); }
            return dst;
        }

        // ─── BLENDING ────────────────────────────────────────────────────────────

        /// <summary>
        /// Berechnet einen Sub-Pixel durch gewichtetes Blending.
        ///
        /// center  = der aktuelle Pixel (F)
        /// edge1   = erster Kanten-Pixel (z.B. B für oben)
        /// edge2   = zweiter Kanten-Pixel (z.B. E für links)
        /// diag1   = Diagonalpixel für schwaches Blending
        /// eqE1    = center == edge1?
        /// eqE2    = center == edge2?
        /// eqD1/D3 = Gleichheiten der Diagonal-Nachbarn
        /// weight  = Diagonalgewicht
        /// farCorner = Pixel 2 Schritte diagonal in Eckenrichtung — bestätigt
        ///             ob dort eine echte Kurve weiterläuft (rundes Objekt)
        ///             oder nicht (dünne Linie) → steuert die Blend-Stärke.
        /// </summary>
        private static int BlendPixel(int center, int edge1, int edge2,
            int diag1,
            bool eqE1, bool eqE2, bool eqD1, bool eqD3,
            float weight, int farCorner)
        {
            // Kein Blending wenn kein eindeutiger Kantenfall vorliegt
            if (!eqE1 && !eqE2) return center;

            // Starke Diagonale → Blending mit Kantenfarbe
            if (weight > 0.6f)
            {
                int edgeColor = eqE1 ? edge1 : edge2;

                // Ecken-Bestätigung: setzt sich die Kurve 2 Pixel weiter fort?
                // Ja  → echte runde Form (Helm, Schild) → stärker runden (87.5%)
                // Nein → vermutlich dünne Diagonal-Linie → konservativ (75%)
                //         bleiben, sonst wirken schmale Details "aufgebläht"
                bool curveConfirmed = Eq(farCorner, edgeColor);
                float blendAmount = curveConfirmed ? 0.875f : 0.75f;
                return BlendRgba(edgeColor, center, blendAmount);
            }

            // Mittlere Diagonale → sanfteres Blending
            if (weight > 0.3f)
            {
                int edgeColor = eqE1 ? edge1 : edge2;
                return BlendRgba(edgeColor, center, 0.5f);
            }

            // Schwache Diagonale → fast kein Blending
            if (eqE1 && eqE2)
            {
                // Beide Kanten gleich center → leichter Blend mit Diagonal-Nachbar
                if (!eqD1 && !eqD3)
                    return BlendRgba(diag1, center, 0.25f);
            }

            return center;
        }

        // ─── DIAGONAL-GEWICHT ────────────────────────────────────────────────────

        /// <summary>
        /// Berechnet wie stark eine Diagonale in der Nachbarschaft ist.
        /// Höherer Wert = stärkere Diagonale = mehr Blending.
        /// </summary>
        private static float DiagWeight(int a, int b, int c, int d,
            int e, int f, int g, int h)
        {
            // Summe der Farbdistanzen entlang beider Diagonalrichtungen
            float diagA = Dist(a, b) + Dist(c, d) + Dist(e, f);
            float diagB = Dist(e, c) + Dist(g, h) + Dist(a, d);

            if (diagA + diagB < 0.001f) return 0f;
            return diagA / (diagA + diagB);
        }

        // ─── FARB-HILFSFUNKTIONEN ────────────────────────────────────────────────

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static bool Eq(int a, int b)
        {
            // Farbgleichheit mit Toleranz für Anti-Aliasing-Pixel
            if (a == b) return true;
            int dr = ((a >> 16) & 0xFF) - ((b >> 16) & 0xFF);
            int dg = ((a >>  8) & 0xFF) - ((b >>  8) & 0xFF);
            int db = ( a        & 0xFF) - ( b        & 0xFF);
            // Alpha-Unterschied ebenfalls prüfen
            int da = ((a >> 24) & 0xFF) - ((b >> 24) & 0xFF);
            return (dr*dr + dg*dg*2 + db*db + da*da) < 64; // Toleranz-Schwellwert
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static float Dist(int a, int b)
        {
            if (a == b) return 0f;
            int dr = ((a >> 16) & 0xFF) - ((b >> 16) & 0xFF);
            int dg = ((a >>  8) & 0xFF) - ((b >>  8) & 0xFF);
            int db = ( a        & 0xFF) - ( b        & 0xFF);
            int da = ((a >> 24) & 0xFF) - ((b >> 24) & 0xFF);
            return (float)Math.Sqrt(dr*dr*2 + dg*dg*4 + db*db + da*da);
        }

        /// <summary>
        /// Lineares Blending zweier ARGB-Pixel.
        /// t=1.0 → komplett a, t=0.0 → komplett b
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int BlendRgba(int a, int b, float t)
        {
            float u = 1f - t;
            int ar = (a >> 16) & 0xFF; int br = (b >> 16) & 0xFF;
            int ag = (a >>  8) & 0xFF; int bg = (b >>  8) & 0xFF;
            int ab = (a       ) & 0xFF; int bb = (b       ) & 0xFF;
            int aa = (a >> 24) & 0xFF; int ba = (b >> 24) & 0xFF;

            int r = Clamp((int)(ar * t + br * u));
            int g = Clamp((int)(ag * t + bg * u));
            int bv= Clamp((int)(ab * t + bb * u));
            int av= Clamp((int)(aa * t + ba * u));

            return (av << 24) | (r << 16) | (g << 8) | bv;
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}

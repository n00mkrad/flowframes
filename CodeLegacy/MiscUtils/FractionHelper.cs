using System;

namespace Flowframes.MiscUtils
{
    internal class FractionHelper
    {
        /// <summary>
        /// Converts a float (<paramref name="value"/>) to an approximated fraction that is as close to the original value as possible, with a limit on the number of digits for numerator and denominator (<paramref name="maxDigits"/>).
        /// </summary>
        public static (int Numerator, int Denominator) FloatToApproxFraction(float value, int maxDigits = 4)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentException("Value must be a finite float.");

            // Special case: zero
            if (Math.Abs(value) < float.Epsilon)
                return (0, 1);

            // Determine the sign and work with absolute value for searching.
            int sign = Math.Sign(value);
            double target = Math.Abs((double)value);

            // Upper bound for numerator/denominator based on max digits
            // e.g. if maxDigits = 4, limit = 9999
            int limit = (int)Math.Pow(10, maxDigits) - 1;

            // We'll track the best fraction found
            double bestError = double.MaxValue;
            int bestNum = 0;
            int bestDen = 1;

            // Simple brute-force search over all possible denominators
            for (int d = 1; d <= limit; d++)
            {
                // Round the numerator for the current denominator
                int n = (int)Math.Round(target * d);

                // If n is 0, skip (except the value might be < 0.5/d, but continue searching)
                if (n == 0)
                    continue;

                // If the numerator exceeds the limit, skip
                if (n > limit)
                    continue;

                // Evaluate how close n/d is to the target
                double fractionValue = (double)n / d;
                double error = Math.Abs(fractionValue - target);

                // If it's closer, record it as our best
                if (error < bestError)
                {
                    bestError = error;
                    bestNum = n;
                    bestDen = d;
                }
            }

            // Reapply the sign to the numerator
            bestNum *= sign;

            // Reduce fraction by GCD (to get simplest form)
            int gcd = GCD(bestNum, bestDen);
            bestNum /= gcd;
            bestDen /= gcd;

            // If the denominator is 1 after reduction, just return the integer
            if (bestDen == 1)
            {
                return (bestNum, 1);
            }

            // Otherwise return "numerator/denominator"
            // Logger.Log($"Approximated fraction for {value}: {bestNum}/{bestDen} (={((float)bestNum / bestDen).ToString("0.0#######")})", true);
            return (bestNum, bestDen);
        }

        /// <summary> Computes the greatest common divisor (Euclid's algorithm). </summary>
        private static int GCD(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }
    }
}

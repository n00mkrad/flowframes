using Flowframes.MiscUtils;
using System;

namespace Flowframes.Data
{
    public class Fraction
    {
        public long Numerator = 0;
        public long Denominator = 1;
        public static Fraction Zero = new Fraction(0, 1);

        public Fraction() { }

        public Fraction(long numerator, long denominator)
        {
            Numerator = numerator;
            Denominator = denominator;

            //If denominator negative...
            if (Denominator < 0)
            {
                //...move the negative up to the numerator
                Numerator = -Numerator;
                Denominator = -Denominator;
            }
        }

        public Fraction(Fraction fraction)
        {
            Numerator = fraction.Numerator;
            Denominator = fraction.Denominator;
        }

        /// <summary>
        /// Initializes a new Fraction by approximating the <paramref name="value"/> as a fraction using up to 4 digits.
        /// </summary>
        public Fraction(float value)
        {
            int maxDigits = 4;
            var (num, den) = FractionHelper.FloatToApproxFraction(value, maxDigits);
            Numerator = num;
            Denominator = den;
        }

        /// <summary>
        /// Initializes a new Fraction from a string <paramref name="text"/>. If the text represents a single number or a fraction, it parses accordingly.
        /// </summary>
        public Fraction(string text)
        {
            try
            {
                if (text.IsEmpty())
                {
                    Numerator = 0;
                    Denominator = 1;
                    return;
                }

                text = text.Replace(':', '/'); // Replace colon with slash in case someone thinks it's a good idea to write a fraction like that
                string[] numbers = text.Split('/');

                // If split is only 1 item, it's a single number, not a fraction
                if (numbers.Length == 1)
                {
                    float numFloat = numbers[0].GetFloat();
                    int numInt = numFloat.RoundToInt();

                    // If parsed float is equal to the rounded int, it's a whole number
                    if (numbers[0].GetFloat().EqualsRoughly(numInt))
                    {
                        Numerator = numInt;
                        Denominator = 1;
                    }
                    else
                    {
                        // Use float constructor if not a whole number
                        var floatFrac = new Fraction(numFloat);
                        Numerator = floatFrac.Numerator;
                        Denominator = floatFrac.Denominator;
                    }

                    return;
                }

                Numerator = numbers[0].GetFloat().RoundToInt();
                Denominator = numbers[1].GetInt();
            }
            catch
            {
                try
                {
                    Numerator = text.GetFloat().RoundToInt();
                    Denominator = 1;
                }
                catch
                {
                    Numerator = 0;
                    Denominator = 1;
                }
            }
        }

        /// <summary>
        /// Calculates and returns the greatest common denominator (GCD) for <paramref name="a"/> and <paramref name="b"/> by dropping negative signs and using the modulo operation.
        /// </summary>
        private static long GetGreatestCommonDenominator(long a, long b)
        {
            //Drop negative signs
            a = Math.Abs(a);
            b = Math.Abs(b);

            //Return the greatest common denominator between two longs
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            if (a == 0)
                return b;
            else
                return a;
        }

        /// <summary>
        /// Calculates and returns the least common denominator for <paramref name="a"/> and <paramref name="b"/> using their greatest common denominator.
        /// </summary>
        private static long GetLeastCommonDenominator(long a, long b)
        {
            return (a * b) / GetGreatestCommonDenominator(a, b);
        }

        /// <summary>
        /// Converts the fraction to have the specified <paramref name="targetDenominator"/> if possible by scaling the numerator accordingly; returns a Fraction with the target denominator or the current fraction if conversion is not possible.
        /// </summary>
        public Fraction ToDenominator(long targetDenominator)
        {
            Fraction modifiedFraction = this;

            // Cannot reduce to smaller denominators & target denominator must be a factor of the current denominator
            if (targetDenominator < Denominator || targetDenominator % Denominator != 0)
                return modifiedFraction;

            if (Denominator != targetDenominator)
            {
                long factor = targetDenominator / Denominator; // Find factor to multiply the fraction by to make the denominator match the target denominator
                modifiedFraction.Denominator = targetDenominator;
                modifiedFraction.Numerator *= factor;
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Reduces the fraction to its lowest terms by repeatedly dividing the numerator and denominator by their greatest common denominator.
        /// </summary>
        public Fraction GetReduced()
        {
            Fraction modifiedFraction = this;

            try
            {
                //While the numerator and denominator share a greatest common denominator, keep dividing both by it
                long gcd = 0;
                while (Math.Abs(gcd = GetGreatestCommonDenominator(modifiedFraction.Numerator, modifiedFraction.Denominator)) != 1)
                {
                    modifiedFraction.Numerator /= gcd;
                    modifiedFraction.Denominator /= gcd;
                }

                //Make sure only a single negative sign is on the numerator
                if (modifiedFraction.Denominator < 0)
                {
                    modifiedFraction.Numerator = -Numerator;
                    modifiedFraction.Denominator = -Denominator;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to reduce fraction ({modifiedFraction}): {e.Message}", true);
            }

            return modifiedFraction;
        }

        /// <summary>
        /// Returns a new Fraction that is the reciprocal of the current fraction by swapping the numerator and denominator.
        /// </summary>
        public Fraction GetReciprocal()
        {
            return new Fraction(Denominator, Numerator);
        }

        /// <summary>
        /// Combines two fractions <paramref name="f1"/> and <paramref name="f2"/> using the specified <paramref name="combine"/> function after converting them to a common denominator; returns the reduced combined Fraction.
        /// </summary>
        private static Fraction Combine(Fraction f1, Fraction f2, Func<long, long, long> combine)
        {
            if (f1.Denominator == 0)
                return f2;
            if (f2.Denominator == 0)
                return f1;

            long lcd = GetLeastCommonDenominator(f1.Denominator, f2.Denominator);
            f1 = f1.ToDenominator(lcd);
            f2 = f2.ToDenominator(lcd);
            return new Fraction(combine(f1.Numerator, f2.Numerator), lcd).GetReduced();
        }

        public override string ToString()
        {
            return $"{Numerator}/{Denominator}";
        }

        // Conversion properties
        public float Float => Denominator < 1 ? 0f : (float)Numerator / (float)Denominator;
        public double Double => (double)Numerator / Denominator;
        public long Long => Denominator < 1 ? 0L : (long)Numerator / (long)Denominator;

        // Operators
        public static bool operator >(Fraction frac, float value) => frac.Double > value;
        public static bool operator <(Fraction frac, float value) => frac.Double < value;
        public static bool operator >(float value, Fraction frac) => value > frac.Double;
        public static bool operator <(float value, Fraction frac) => value < frac.Double;
        public static Fraction operator +(Fraction frac1, Fraction frac2) => Combine(frac1, frac2, (a, b) => a + b);
        public static Fraction operator -(Fraction frac1, Fraction frac2) => Combine(frac1, frac2, (a, b) => a - b);
        public static Fraction operator *(Fraction frac1, Fraction frac2) => new Fraction(frac1.Numerator * frac2.Numerator, frac1.Denominator * frac2.Denominator).GetReduced();
        public static Fraction operator /(Fraction frac1, Fraction frac2) => new Fraction(frac1 * frac2.GetReciprocal()).GetReduced();
        public static Fraction operator *(Fraction frac, long mult) => new Fraction(frac.Numerator * mult, frac.Denominator).GetReduced();
        public static Fraction operator *(Fraction frac, double mult) => new Fraction((long)Math.Round(frac.Numerator * mult), frac.Denominator).GetReduced();
        public static Fraction operator *(Fraction frac, float mult) => new Fraction((frac.Numerator * mult).RoundToInt(), frac.Denominator).GetReduced();

        public string GetString(string format = "0.#####")
        {
            return ((float)Numerator / Denominator).ToString(format);
        }
    }
}

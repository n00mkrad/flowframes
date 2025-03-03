﻿using Flowframes.MiscUtils;
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

        public Fraction(float value)
        {
            int maxDigits = 4;
            var (num, den) = FractionHelper.FloatToApproxFraction(value, maxDigits);
            Numerator = num;
            Denominator = den;
        }

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

        private static long GetGreatestCommonDenominator(long a, long b)
        {
            //Drop negative signs
            a = Math.Abs(a);
            b = Math.Abs(b);

            //Return the greatest common denominator between two longegers
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

        private static long GetLeastCommonDenominator(long a, long b)
        {
            //Return the Least Common Denominator between two longegers
            return (a * b) / GetGreatestCommonDenominator(a, b);
        }


        public Fraction ToDenominator(long targetDenominator)
        {
            //Multiply the fraction by a factor to make the denominator
            //match the target denominator
            Fraction modifiedFraction = this;

            //Cannot reduce to smaller denominators
            if (targetDenominator < Denominator)
                return modifiedFraction;

            //The target denominator must be a factor of the current denominator
            if (targetDenominator % Denominator != 0)
                return modifiedFraction;

            if (Denominator != targetDenominator)
            {
                long factor = targetDenominator / Denominator;
                modifiedFraction.Denominator = targetDenominator;
                modifiedFraction.Numerator *= factor;
            }

            return modifiedFraction;
        }

        public Fraction GetReduced()
        {
            //Reduce the fraction to lowest terms
            Fraction modifiedFraction = this;

            try
            {
                //While the numerator and denominator share a greatest common denominator,
                //keep dividing both by it
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

        public Fraction GetReciprocal()
        {
            //Flip the numerator and the denominator
            return new Fraction(Denominator, Numerator);
        }


        public static Fraction operator +(Fraction fraction1, Fraction fraction2)
        {
            //Check if either fraction is zero
            if (fraction1.Denominator == 0)
                return fraction2;
            else if (fraction2.Denominator == 0)
                return fraction1;

            //Get Least Common Denominator
            long lcd = GetLeastCommonDenominator(fraction1.Denominator, fraction2.Denominator);

            //Transform the fractions
            fraction1 = fraction1.ToDenominator(lcd);
            fraction2 = fraction2.ToDenominator(lcd);

            //Return sum
            return new Fraction(fraction1.Numerator + fraction2.Numerator, lcd).GetReduced();
        }

        public static Fraction operator -(Fraction fraction1, Fraction fraction2)
        {
            //Get Least Common Denominator
            long lcd = GetLeastCommonDenominator(fraction1.Denominator, fraction2.Denominator);

            //Transform the fractions
            fraction1 = fraction1.ToDenominator(lcd);
            fraction2 = fraction2.ToDenominator(lcd);

            //Return difference
            return new Fraction(fraction1.Numerator - fraction2.Numerator, lcd).GetReduced();
        }

        public static Fraction operator *(Fraction fract, long multi)
        {
            long numerator = (long)fract.Numerator * (long)multi;
            long denomenator = fract.Denominator;

            return new Fraction(numerator, denomenator).GetReduced();
        }

        public static Fraction operator *(Fraction fract, double multi)
        {
            long numerator = (long)Math.Round((double)(fract.Numerator * (double)multi));
            long denomenator = fract.Denominator;

            return new Fraction(numerator, denomenator).GetReduced();
        }

        public static Fraction operator *(Fraction fract, float multi)
        {
            long numerator = (fract.Numerator * multi).RoundToInt();
            long denomenator = fract.Denominator;

            return new Fraction(numerator, denomenator).GetReduced();
        }

        public static Fraction operator *(Fraction fraction1, Fraction fraction2)
        {
            long numerator = fraction1.Numerator * fraction2.Numerator;
            long denomenator = fraction1.Denominator * fraction2.Denominator;

            return new Fraction(numerator, denomenator).GetReduced();
        }

        public static Fraction operator /(Fraction fraction1, Fraction fraction2)
        {
            return new Fraction(fraction1 * fraction2.GetReciprocal()).GetReduced();
        }

        public double Double() => (double)Numerator / Denominator;

        public override string ToString()
        {
            return $"{Numerator}/{Denominator}";
        }

        public float Float => Denominator < 1 ? 0f : (float)Numerator / (float)Denominator;
        public long Long => Denominator < 1 ? 0L : (long)Numerator / (long)Denominator;

        public string GetString(string format = "0.#####")
        {
            return ((float)Numerator / Denominator).ToString(format);
        }
    }
}

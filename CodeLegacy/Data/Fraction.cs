using System;
using System.Windows.Navigation;

namespace Flowframes.Data
{
    public struct Fraction
    {
        public long Numerator;
        public long Denominator;
        public static Fraction Zero = new Fraction(0, 0);

        public Fraction(long numerator, long denominator)
        {
            this.Numerator = numerator;
            this.Denominator = denominator;

            //If denominator negative...
            if (this.Denominator < 0)
            {
                //...move the negative up to the numerator
                this.Numerator = -this.Numerator;
                this.Denominator = -this.Denominator;
            }
        }

        public Fraction(long numerator, Fraction denominator)
        {
            //divide the numerator by the denominator fraction
            this = new Fraction(numerator, 1) / denominator;
        }

        public Fraction(Fraction numerator, long denominator)
        {
            //multiply the numerator fraction by 1 over the denominator
            this = numerator * new Fraction(1, denominator);
        }

        public Fraction(Fraction fraction)
        {
            Numerator = fraction.Numerator;
            Denominator = fraction.Denominator;
        }

        public Fraction(float value)
        {
            Numerator = (value * 10000f).RoundToInt();
            Denominator = 10000;
            this = GetReduced();
        }

        public Fraction(string text)
        {
            try
            {
                string[] numbers = text.Split('/');
                Numerator = numbers[0].GetInt();
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
                    Denominator = 0;
                }
            }

            Console.WriteLine($"Fraction from String: Fraction(\"{text}\") => {Numerator}/{Denominator}", true);
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
            if (targetDenominator < this.Denominator)
                return modifiedFraction;

            //The target denominator must be a factor of the current denominator
            if (targetDenominator % this.Denominator != 0)
                return modifiedFraction;

            if (this.Denominator != targetDenominator)
            {
                long factor = targetDenominator / this.Denominator;
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
                    modifiedFraction.Numerator = -this.Numerator;
                    modifiedFraction.Denominator = -this.Denominator;
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
            return new Fraction(this.Denominator, this.Numerator);
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


        public double ToDouble()
        {
            return (double)this.Numerator / this.Denominator;
        }

        public override string ToString()
        {
            return Numerator + "/" + Denominator;
        }

        public float GetFloat()
        {
            if (Denominator < 1)    // Avoid div by zero
                return 0f;

            return (float)Numerator / (float)Denominator;
        }

        public long GetLong()
        {
            return (long)Numerator / (long)Denominator;
        }

        public string GetString()
        {
            return ((float)Numerator / Denominator).ToString();
        }
    }
}

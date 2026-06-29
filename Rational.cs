using System.Numerics;

namespace RvB.Puzzles.Shared;

/// <summary>
/// A simple implementation for a rational number.
/// </summary>
public readonly record struct Rational : IComparable<Rational>, IMinMaxValue<Rational> {
    private readonly long _numerator;
    private readonly long _denominator;

    public static Rational Zero { get; } = new(0, 0); // So it's equivalent to default(Rational)

    public static Rational One { get; } = new(1, 1);

    public static Rational MaxValue { get; } = new(long.MaxValue, 1);

    public static Rational MinValue { get; } = new(long.MinValue, 1);

    public long Numerator => _numerator;

    public long Denominator => _numerator == 0 ? 1 : _denominator;

    public Rational(long numerator, long denominator) {
        if (numerator == 0) {
            _numerator = _denominator = 0;
        } else if (denominator == 0) {
            throw new DivideByZeroException();
        } else {
            if (denominator < 0) {
                numerator = -numerator;
                denominator = -denominator;
            }
            if (denominator > 1) {
                var gcd = Math.Abs(MathEx.GCD(numerator, denominator));
                _numerator = numerator / gcd;
                _denominator = denominator / gcd;
            } else {
                _numerator = numerator;
                _denominator = denominator;
            }
        }
    }

    public static Rational operator +(Rational value) {
        return value;
    }

    public static Rational operator ++(Rational value) {
        checked {
            return new(value.Numerator + value.Denominator, value.Denominator);
        }
    }

    public static Rational operator -(Rational value) {
        checked {
            return new(-value.Numerator, value.Denominator);
        }
    }

    public static Rational operator --(Rational value) {
        checked {
            return new(value.Numerator - value.Denominator, value.Denominator);
        }
    }

    public static Rational operator +(Rational left, Rational right) {
        checked {
            return new((left.Numerator * right.Denominator) + (left.Denominator * right.Numerator), left.Denominator * right.Denominator);
        }
    }

    public static Rational operator -(Rational left, Rational right) {
        checked {
            return new((left.Numerator * right.Denominator) - (left.Denominator * right.Numerator), left.Denominator * right.Denominator);
        }
    }

    public static Rational operator *(Rational left, Rational right) {
        checked {
            return new(left.Numerator * right.Numerator, left.Denominator * right.Denominator);
        }
    }

    public static Rational operator /(Rational left, Rational right) {
        checked {
            return new(left.Numerator * right.Denominator, left.Denominator * right.Numerator);
        }
    }

    public static Rational operator %(Rational left, Rational right) {
        checked {
            return new(left._numerator * right._denominator % (left._denominator * right._numerator), left._denominator * right._denominator);
        }
    }

    public static explicit operator long(Rational value)
        => value.Numerator / value.Denominator;

    public static explicit operator int(Rational value) {
        checked {
            return (int)(value.Numerator / value.Denominator);
        }
    }

    public static implicit operator Rational(long value)
        => new(value, 1);

    public static implicit operator Rational(int value)
        => new(value, 1);

    public override string ToString() {
        if (Denominator != 1)
            return $"{Numerator:n0}/{Denominator:n0}";
        return $"{Numerator:n0}";
    }

    public int CompareTo(Rational other) {
        checked {
            var l = Numerator * other.Denominator;
            var r = other.Numerator * Denominator;
            return l.CompareTo(r);
        }
    }
}

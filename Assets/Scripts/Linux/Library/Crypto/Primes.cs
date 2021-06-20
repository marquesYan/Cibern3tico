using System;
using System.Numerics;

namespace Linux.Library.Crypto {
    public class Primes {
        public static bool IsPrime(BigInteger number, int rounds) {
            // See https://gist.github.com/Ayrx/5884790

            var rand = new Random();

            if (number == 2) {
                return true;
            }

            if ((number % 2) == 0) {
                return false;
            }

            int r, s;
            r = 0;
            s = (int)number - 1;

            while ((s % 2) == 0) {
                r++;
                s /= 2;
            }

            BigInteger random, temp;

            for (double i = 0; i < rounds; i++) {
                random = new BigInteger(rand.Next(2, (int)number - 1));

                temp = BigInteger.ModPow(random, s, number);

                if (temp == 1 || temp == number - 1) {
                    continue;
                }

                if ((r - 1) < 0) {
                    return false;
                }

                for (double j = 0; j < r - 1; j++) {
                    temp = BigInteger.ModPow(temp, 2, number);

                    if (temp == (number - 1)) {
                        break;
                    }
                }
            }

            return true;
        }

        public static BigInteger RandomPrime(
            int minValue,
            int maxValue,
            int rounds
        ) {
            var rand = new Random();
            
            BigInteger prime;
            while (true) {
                prime = new BigInteger(rand.Next(minValue, maxValue));

                if (IsPrime(prime, rounds)) {
                    return prime;
                }
            }
        }
    }
}
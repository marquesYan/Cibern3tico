using System;
using UnityEngine;

namespace Linux.Library.Crypto {
    public class Primes {
        public static bool IsPrime(double number, double rounds) {
            // See https://gist.github.com/Ayrx/5884790

            var rand = new System.Random();

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

            double random, temp;

            for (double i = 0; i < rounds; i++) {
                random = Convert.ToDouble(rand.Next(2, (int)number - 1));
                temp = Math.Pow(random, s) % number;

                if (temp == 1 || temp == number - 1) {
                    continue;
                }

                if ((r - 1) < 0) {
                    return false;
                }

                for (double j = 0; j < r - 1; j++) {
                    temp = Math.Pow(temp, 2) % number;

                    if (temp == (number - 1)) {
                        break;
                    }
                }
            }

            return true;
        }

        public static double GreatestCommonDivisor(double a, double b) {
            double remainer;

            while (true) {
                remainer = a % b;

                if (remainer == 0) {
                    return b;
                }

                a = b;
                b = remainer;
            }
        }

        public static double RandomPrime(int rounds) {
            var rand = new System.Random();
            
            double prime;
            while (true) {
                prime = Convert.ToDouble(rand.Next(1024, 65536));

                if (IsPrime(prime, rounds)) {
                    return prime;
                }
            }
        }
    }
}
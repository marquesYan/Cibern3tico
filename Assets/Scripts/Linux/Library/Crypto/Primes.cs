using System;
using System.Numerics;
using UnityEngine;

namespace Linux.Library.Crypto {
    public class Primes {
        // For implementation reference:
        // https://github.com/sybrenstuvel/python-rsa/blob/main/rsa/prime.py

        public static bool MillerRabinPrimalityTesting(
            int n,
            int k
        ) {
            // Reference:
            // https://en.wikipedia.org/wiki/Miller%E2%80%93Rabin_primality_test

            if (n < 2) {
                return false;
            }

            int d = n - 1;
            int r = 0;

            while ((d & 1) == 0) {
                r++;
                d >>= 1;
            }

            int random;
            double x;
            var rand = new System.Random();

            for (int i = 0; i < k; i++) {
                random = rand.Next(n - 3) + 1;

                x = Math.Pow(random, d) % n;

                if (x == 1 || x == (n - 1)) {
                    continue;
                }

                bool isComposite = true;

                for (int j = 0; j < (r - 1); j++) {
                    x = Math.Pow(x, 2) % n;
                    if (x == 1) {
                        return false;
                    }

                    if (x == (n - 1)) {
                        isComposite = false;
                        break;
                    }
                }

                if (isComposite) {
                    return false;
                }
            }

            return true;
        }

        public static bool IsPrime(int number) {
            int k = 10;
            return MillerRabinPrimalityTesting(number, k);         
        }

        public static int RandomPrime(
            int minValue,
            int maxValue
        ) {
            var rand = new System.Random();
            
            int prime;
            while (true) {
                prime = rand.Next(minValue, maxValue);

                if (IsPrime(prime)) {
                    return prime;
                }
            }
        }
    }
}
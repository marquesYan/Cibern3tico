using System;
using System.Numerics;
using UnityEngine;

namespace Linux.Library.Crypto {
    public class RSA {
        public BigInteger N { get; protected set; }
        
        public BigInteger D { get; protected set; }

        public BigInteger E { get; protected set; }

        public RSA(BigInteger n, BigInteger d, BigInteger e) {
            N = n;
            D = d;
            E = e;
        }

        public static RSA PubKey(BigInteger n, BigInteger e) {
            return new RSA(n, -1, e);
        }

        public static RSA PrivKey(BigInteger n, BigInteger d) {
            return new RSA(n, d, -1);
        }

        public static BigInteger LeastCommonMultiple(
            BigInteger a,
            BigInteger b
        ) {
            // See https://www.geeksforgeeks.org/program-to-find-lcm-of-two-numbers/

            return BigInteger.Multiply(
                BigInteger.Divide(a, BigInteger.GreatestCommonDivisor(a, b)),
                b
            );
        }

        public static RSA New() {
            // See https://en.wikipedia.org/wiki/RSA_%28algorithm%29#Key_generation
  
            int rounds = 3;
            int minValue = 64;
            int maxValue = 512;

            BigInteger p = Primes.RandomPrime(
                minValue,
                maxValue,
                rounds
            );

            BigInteger q = Primes.RandomPrime(
                minValue,
                maxValue,
                rounds
            );
  
            BigInteger n = BigInteger.Multiply(p, q);

            BigInteger phi = LeastCommonMultiple((p - 1), (q - 1));

            // Encryption
            BigInteger e = 2;

            while (true) {
                if (BigInteger.GreatestCommonDivisor(e, phi) == 1) {
                    break;
                }

                e++;
            }

            // Decryption
            BigInteger d = BigInteger.ModPow(
                e,
                phi -1,
                phi
            );

            return new RSA(n, d, e);
        }

        public BigInteger Encrypt(BigInteger number) {
            if (E == -1) {
                throw new InvalidOperationException(
                    "Can not encrypt"
                );
            }

            return BigInteger.ModPow(number, E, N);
        }

        public BigInteger Decrypt(BigInteger number) {
            if (D == -1) {
                throw new InvalidOperationException(
                    "Can not decrypt"
                );
            }

            return BigInteger.ModPow(number, D, N);
        }
    }
}
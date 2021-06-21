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

        public static RSA FromParameters(
            BigInteger p,
            BigInteger q,
            BigInteger e,
            BigInteger phi
        ) {
            BigInteger n = BigInteger.Multiply(p, q);

            // Decryption
            BigInteger d = BigInteger.ModPow(
                e,
                phi -1,
                phi
            );

            return new RSA(n, d, e);
        }

        public static RSA New() {
            // See https://en.wikipedia.org/wiki/RSA_%28algorithm%29#Key_generation
  
            int minValue = 128;
            int maxValue = 1024;

            var rand = new System.Random();

            BigInteger p = new BigInteger(
                Primes.RandomPrime(
                    minValue,
                    maxValue
                )
            );

            BigInteger q = new BigInteger(
                Primes.RandomPrime(
                    minValue,
                    maxValue
                )
            );

            BigInteger phi = LeastCommonMultiple((p - 1), (q - 1));

            // Encryption
            BigInteger e;
            while (true) {
                e = rand.Next(1, (int)phi);

                if (BigInteger.GreatestCommonDivisor(e, phi) == 1) {
                    break;
                }
            }

            return FromParameters(p, q, e, phi);
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
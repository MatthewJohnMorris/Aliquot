# Aliquot
Aliquot Analysis (Chains, Trees, etc)

This is a Microsoft Visual Studio Solution, aimed at helping with analysis of Aliquot sequences.

Aliquot sequences are got by summing across all proper divisors of a number to get the Aliquot successor.

Perfect numbers (such as 6) have themselves as an Aliquot successor, while Prime numbers will have 1 as Aliquot successor.

There is an open conjecture that all Aliquot sequences terminate in a prime, a perfect number or a sociable cycle.

Construcing an Aliquot sequence requires arriving at prime factorisations. To assist with this, we generate and store all 32-bit primes in a compressed file.

We can also write a "database" of analyzed numbers, which can then be further post-processed to arrive at graphs of trees etc.

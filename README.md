# BloomFilters
Bloom filters focused on detecting set differences. Written in C#.

This work is in very early stages, exploring options, performance and characteristics.  The first approach is based upon invertible Bloom filters, as described in http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf , but with the following notable modifications:

1. Detect not only differences between the identifier sets, but also differences between values.
2. Add an option for splitting the hash values out in separate buckets (positive or negative impact is to be reviewed).

Included with the Bloom Filter is a strate estimator, as described in the above paper. Final goal is to implement a hybrid strata estimator, utilizing the b-bit min hash (described in http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf) for larger differences.

Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.

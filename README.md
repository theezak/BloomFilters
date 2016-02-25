# BloomFilters
Bloom filters focused on detecting set differences. Written in C#.

This work is in very early stages, exploring options, performance and characteristics.  The first approach is based upon invertible Bloom filters, as described in http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf , but with the following notable modifications:

1. Detect changes in value as well differences between the identifier sets.
2. Add an option for splitting the hash values out in separate buckets (positive or negative impact is to be reviewed).

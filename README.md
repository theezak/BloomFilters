# Invertible Bloom Filters
Bloom filters focused on detecting set differences. Written in C#.

This work is in very early stages, exploring options, performance and characteristics.  The first approach is based upon invertible Bloom filters, as described in http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf . A Bloom filter is presented that with a substraction operation to determine the difference between two Bloom filters. Although the article presents a Bloom filter that stores key/value paris, the decode algorithm presented does not actually detect changes in the value. The following additions were made in this implementation to account for this:

1. Any pure items that after substraction have count zero, but do not have zero for the value hash, will have their Id added to the set of differences.
2. During decoding, a list will be kept of all locations that have the Id of a pure item substracted. If any of the locations finally end with count equal to zero, but have a hash value that does not zero out, all identifiers in the list for that location will be added to the set of differences. Note that items that have not changed might be added, but any of the items is a potential candidate for causing the mismatch.

Additionaly, an option for splitting the hash values out in separate buckets has been added (positive or negative impact is to be reviewed).

Included with the Bloom Filter is a strata estimator (as described in the above paper). Final goal is to implement a hybrid strata estimator, utilizing the b-bit minwise hash (described in http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf) for larger differences.

The estimator is important, because an estimate of the number of differences is needed to pick a proper sized Bloom filter that can be decoded. When the estimate is too large, a Bloom filter will be used that requires more space than needed. When the estimate is too small, a Bloom filter might be used that can't be successfully decoded, resulting in additional space and time being needed to find a Bloom filter that is properly sized.

## Serialization
Support has been added for serializing and deserializing Bloom filters and estimators.

## Size for Count
When utilizing an invertible Bloom filter within the capacity it was sized for, the count will seldom exceed 2 or 3. However, when utilizing estimators, the idea is that the invertible Bloom filter will be utilized at a much higher capacity than it was sized for, thus accepting a higher error rate. To account for both scenario's, the actual count type is configurable. Two types will be supported out of the box: sbyte and int.
Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.

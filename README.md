# Invertible Bloom Filters
Bloom filters focused on detecting set differences. Written in C#.

This work is in very early stages, exploring options, performance and characteristics.The goal is to efficiently determine the differences between two sets of key/value pairs. 

The first approach is based upon invertible Bloom filters, as described in http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf . In this paper an invertible Bloom filter is presented with a subtraction operator that determines the difference between two Bloom filters. After implementing the data structure as described in the paper, it was noted that the data structure detected changes in the keys, but did not detect changes in the values. The following additions were made to account for changes in the values as well:

1. Any pure items that after substraction have count zero, but do not have zero for the value hash, will have their Id added to the set of differences. Intuitively this means any item stored by itself in the same location across both Bloom filters, has a different value when the hash of the values is different.
2. During decoding, a list will be kept of all locations that have the Id of a pure item substracted. If any of the locations finally end with count equal to zero, but have a hash value that does not zero out, all identifiers in the list for that location will be added to the set of differences. Intuitively this means that during decoding we associate any identifier that is pure with all the locations of the identifier. When a position does not decode correctly, we then utilize the knowledge of the (partial) list of items associated with that position. Note that items that have not changed might be added, but any of the items is a potential candidate for causing the mismatch.

Additionaly, an option was added for splitting the hash values out in a bucket per hash function. Testing however showed that splitting the hash values per hash function caused a significant increase in the error rate.

Included with the Bloom Filter is a strata estimator (as described in the above paper). Based upon the strata estimator, a hybrid strata estimator was implemented utilizing the b-bit minwise hash (described in http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf). Testing showed that a capacity of 15 with a strata of 7 is ideal in most cases, except for extremely large sets with a high expected difference, in which case a capacity of 1000 and a strata of 13 performed well.

The estimator is important, because an estimate of the number of differences is needed to pick a proper sized Bloom filter that can be decoded. When the estimate is too large, a Bloom filter will be used that requires more space than needed. When the estimate is too small, a Bloom filter might be used that can't be successfully decoded, additional space and time is required to find a Bloom filter that is large enough to be succesfully decoded.

## Serialization
Support has been added for serializing and deserializing Bloom filters and estimators.

## Overloading a Bloom filter
When utilizing an invertible Bloom filter within the capacity it was sized for, the count will seldom exceed 2 or 3. However, when utilizing estimators, the idea is that the invertible Bloom filter will be utilized at a much higher capacity than it was sized for, thus accepting a higher error rate. To account for both scenario's, the actual count type is configurable. Two types will be supported out of the box: sbyte and int.

## Wishlist
Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.

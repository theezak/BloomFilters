# Invertible Bloom Filters
On using invertible Bloom filters for estimating differences between sets of key/value pairs. Written in C#.

This work is in very early stages, exploring options, performance and characteristics.The goal is to efficiently determine the differences between two sets of key/value pairs. 

The first approach is based upon invertible Bloom filters, as described in "What’s the Difference? Efﬁcient Set Reconciliation without Prior Context" (David Eppstein, Michael T. Goodrich, Frank Uyeda, George Varghese, 2011, http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf) . A similar data structure, but not extended to detect set differences, is described in "Invertible Bloom Lookup Tables" (Michael T. Goodrich, Michael Mitzenmacher, 2015, http://arxiv.org/pdf/1101.2245v3.pdf). In this paper an invertible Bloom filter is presented with a subtraction operator that determines the difference between two Bloom filters. After implementing the data structure as described in the paper, it was noted that the data structure detected changes in the keys, but did not detect changes in the values. The following additions were made to account for changes in the values as well:

1. Any pure items that after substraction have count zero, but do not have zero for the value hash, will have their Id added to the set of differences. Intuitively this means any item stored by itself in the same location across both Bloom filters, has a different value when the hash of the values is different.
2. During decoding, any of the locations finally ending up with count equal to zero, will be evaluated for non zero identifier hashes or non zero value hashes. Intuitively this means that during decoding we can identify difference not only from pure locations, but also from locations that transition from pure to zero.

Included with the Bloom Filter is a strata estimator (as described in the above paper). Based upon the strata estimator, a hybrid strata estimator was implemented utilizing the b-bit minwise hash (described in http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf). 

The estimator is important, because an estimate of the number of differences is needed to pick a proper sized Bloom filter that can be decoded. The size of the invertible Bloom filter needed for detecting the changes between two sets is not dependent upon the set sizes, but upon the size of the difference.  For example, 30 differences between two sets of 500000 elements each can be fully detected by 63 kilobyte invertible Bloom filter. On the other hand, 40000 differences between two sets of 45000 items each can take a Bloom filter of 17 megabytes. 

When the estimate for the difference is too large, a Bloom filter will be used that requires more space than needed. When the estimate is too small, a Bloom filter might be used that can't be successfully decoded, additional space and time is required to find a Bloom filter that is large enough to be succesfully decoded.

## Serialization
Support has been added for serializing and deserializing Bloom filters and estimators.

## Overloading a Bloom filter
When utilizing an invertible Bloom filter within the capacity it was sized for, the count will seldom exceed 2 or 3. However, when utilizing estimators, the idea is that the invertible Bloom filter will be utilized at a much higher capacity than it was sized for, thus accepting a higher error rate and much higher count values. To account for both scenario's, the actual count type is configurable. Two types will be supported out of the box: sbyte and int.

## Empirical data
Testing showed that a capacity of 15 with a strata of 7 is ideal in most cases, except for extremely large sets with a high expected difference, in which case a capacity of 1000 and a strata of 13 performed well.

Invertible Bloom filters of relatively low capacity (total size less than 300 kilobytes) performed very well on medium sized sets (1000) with a lower number of differences (50) for detecting differences between keys in both sets. Invertible Bloom filters of much higher capacity (2* size**2) were needed to detect differences in the values of keys that were in both sets. The conclusion was that the invertible Bloom filter for key/value pairs as presented did not perform well on detecting a different value for a key across the sets. This can be easily explained based upon how the invertible Bloom filter functions. The invertible Bloom filter focuses on the keys and their counts, while detecting differences in the values only becomes possible for pure locations, which requires very sparse (high capacity) Bloom filters. When the Bloom filter does not have a high capacity, the keys and counts will zero out, but any value differences will not, thus causing decoding errors.

A rather simple solution to this challenge would be to replace the key/value pairs, by a single hashed value. The crux to detecting the set differences is however the ability to recover the key values for the items in the difference. Since we can't recover the identifier from a combined hash value, we have to store the identifier individually in the Bloom filter. We could consider keeping a reverse mapping from hashed value to identifier in each location, but this can only be done when it is acceptable to not know the identifiers in the set difference in other locations.

Combining the outcomes of these initial results, the following adjustments were made:
- Estimators should store a single hash of the key and value combined. This reflects that estimators considers a key/value pair different when the key or the value is different. A significant reduction in the size of the estimator can be accomplished this way, since the strata estimators no longer require hashsum storage. 
- The invertible Bloom filter can internally utilize a 'reverted' invertible Bloom filter for storing values. The values become identifiers, and the keys become the hashsums. This approach will require additional storage (about 2 times, but by dropping the hashsum value from the key Bloom filter, we can reduce this to about 5/3). The advantage should however be that differences between values can be identified at much lower capacities.

Initial testing has shown that the proposed invertible Bloom filter is highly capable of detecting key/value pair differences with a Bloom filter capacity equal to the estimated number of differences. Estimators require consistently 26K. The Bloom filter size for 1000 differences is 216KB, with 11000 differences requiring a filter of 1.3MB. No differences were missed, but there is over identification of differences. Further analysis is needed.

As an alternative, the 'reverse' invertible Bloom filter by itself performs well. The 'reverse' invertible Bloom filter can miss some keys that are unique to a set, but overall shows higher performance than the regular invertible Bloom filter at the same size.

## Wishlist
Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.


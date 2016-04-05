# Invertible Bloom Filters
On using invertible Bloom filters for estimating differences between sets of key/value pairs. Written in C#.

The goal is to efficiently determine the differences between two sets of key/value pairs. The first approach is based upon invertible Bloom filters, as described in "What’s the Difference? Efﬁcient Set Reconciliation without Prior Context" (David Eppstein, Michael T. Goodrich, Frank Uyeda, George Varghese, 2011, http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf) . A similar data structure, but not extended to detect set differences, is described in "Invertible Bloom Lookup Tables" (Michael T. Goodrich, Michael Mitzenmacher, 2015, http://arxiv.org/pdf/1101.2245v3.pdf). 

After implementing the data structure, it was noted that the data structure detected changes in the keys, but did not perform equally well at detecting changes in the values. An alternative solution is presented in the form of a reverse invertible Bloom filter (see in https://drive.google.com/file/d/0B1bvyH2cU0m4N3BEWWQxWV9PUmc/view?usp=sharing ).

Included with the reverse invertible Bloom Filter implementation is a strata estimator (as described in http://conferences.sigcomm.org/sigcomm/2011/papers/sigcomm/p218.pdf ). Based upon the strata estimator, a hybrid strata estimator was implemented utilizing the b-bit minwise hash (see http://research.microsoft.com/pubs/120078/wfc0398-liPS.pdf). 

The estimator is important, because an estimate of the number of differences is needed to pick a proper sized Bloom filter that can be decoded. The size of the invertible Bloom filter needed for detecting the changes between two sets is not dependent upon the set sizes, but upon the size of the difference.  For example, 30 differences between two sets of 500000 elements each can be fully detected by a 5kb invertible Bloom filter. On the other hand, 40000 differences between two sets of 60000 items each can take a Bloom filter of 3.3 megabytes. 

When the estimate for the difference is too large, a Bloom filter will be used that requires more space than needed. When the estimate is too small, a Bloom filter might be used that can't be successfully decoded, additional space and time is required to find a Bloom filter that is large enough to be succesfully decoded.

## Resizing a Bloom filter

A Bloom filter can be folded through any factor of its size. For example: a Bloom filter with 500 cells can be folded by the following factors: 1, 2, 4, 5, 10, 20, 25, 50, 100, 125, 250, 500. A strategy is provided for folding, including a strategy that sizes Bloom filter with smooth numbers to increase the folding opportunities. Based upon the folding operator, a compression operator is provided that attempts to reduce the size of a Bloom filter utilized under its capacity without impacting its error rate. Similar fold and compress operations are provided for estimators.

## Serialization

Support has been added for serializing and deserializing Bloom filters and estimators. You can extract the data in a serializable format from both the Bloom filters and the estimators. In general you can also load the extracted data back into the Bloom filter or estimator. An exception to this rule is the bit minwise estimator, and thus the hybrid estimator. Since the bit minwise estimator only extracts b bits of each cell, you can't restore the estimator from the data. An alternative full extract is provided for both the bit minwise estimator and hybrid estimator, that includes all bits for each cell.

## Overloading a Bloom filter

When utilizing an invertible Bloom filter within the capacity it was sized for, the count will seldom exceed 2 or 3. However, when utilizing estimators, the idea is that the invertible Bloom filter will be utilized at a much higher capacity than it was sized for, thus accepting a higher error rate and much higher count values. To account for both scenario's, the actual count type is configurable. Four types are supported out of the box: sbyte, short, int and long. Always ensure that the count type used can handle highly folded filters. When the difference between two sets is small, there can be as few as 30 or 40 cells whose combined count equals the total set size. The counters will not overflow, but stay at maximum or minimum value. Overflow scenario's will however negatively impact results and performance. It is better to choose a slightly larger count type and be able to utilize folding, than it is to use a smaller count type and encounter overflows.

## Steps for calculating the set difference

When two parties, A and B, want to determine their set difference, one of the parties, lets say party A, initiates the process by sending an estimator that contains all items in its set. The estimator provides a low overhead, that can be shown to be at most in the order of the difference between the two sets. Party B will compare the estimator against its local estimator and generate an estimate of the size of the difference. 

The second step exists of party B sending a Bloom filter containing all its elements, but with a capacity equal to the size of the difference. The overhead of this communication is in the order of the number of differences. Party A receives this Bloom filter and subtracts it from its local Bloom filter. After decoding the result of the subtraction, party A will have three sets of keys: keys for elements only at party B, keys for elements only at party A and keys for elements that have the same key at party A and party B, but have a different value across party A and party B. 

The final step is exchanging the items that were different between party A and party B, which again can be done in the order of the number of differences found.

## Computational overhead
The cost of computing the value hash can be considerable. If this becomes an issue, a dictionary of keys and pre-computed hash values can be maintained (for example in a NoSQL store). An example of the key-value Bloom filter configuration needed to utilize these precomputed values has been included.

The estimators and Bloom filters themselves can in fact be pre-computed under certain conditions and persisted if needed. Both the estimator and invertible Bloom filter support inserts and removal of items. Removal does come at a price for a hybrid estimator: the bit minwise estimator does not support removal of items, so at the first removal the bit minwise estimator is replaced by a simple count. Any differences found across the Bloom filters in the hybrid estimator are then proportionally extended across the simple count that replaces the bit minwise estimator. Since the bit minwise estimator typically only contains a very small fraction of the items, this does not impact the estimate in a significant way.

## A practical implementation of pre-calculated estimators and filters

A practial scenario would be that all parties agree upon a pre-determined size for the estimator and the filter, typically larger than the maximum size anticipated. The following scenario's are possible:
- The estimator sent has at least the required capacity and strata count needed by the receiver to represent the set. The two estimators will decode correctly and provide an estimate.
- The estimator sent does not have a large enough capacity or large enough strata for the receiver their dataset. The receiving party has two options:
    - Send their local estimator back. This estimator will have the capacity needed and the other party will be able to calculate an estimate.
    - Assume that a failed decode means that the difference is large and close to the total item count of the Bloom filter. You can choose to send the whole Bloom filter (possibly still trading recognized differences versus some level of folding).

When the two estimators provide an estimate, you can send the local Bloom filter, folded to be in the order of size of the estimate. When the two filters cannot be subtracted and decoded, the filters are not compatible. The parties should agree on a new precalculated filter size. It is important to remember in this process that:
- Estimators are helpful in avoiding over-sized Bloom filters, but you can exchange Bloom filters even without an estimate. There is just a higher risk of either wasting band width or only recognizing a relatively small fraction of the differences.
- Bloom filters that are not under sized will still yield some differences, and thus provide progress toward eliminating the differences.
- The only major obstacle occurs when the Bloom filters are not compatible, for example because they can't be resized to have the same size (no shared factor), have different hash functions or have different hash counts. A minor obstacle is two estimators that do not have a shared factor, but you could still choose to exchange the Bloom filters.

The solution provided in this repository includes a test implementation of pre-calculated estimators and filters.

## Wishlist
Although this is initially just a testbed, an obvious wishlist item is a buffer pool to counteract some of the horrible things the Bloom Filter does to memory management.

